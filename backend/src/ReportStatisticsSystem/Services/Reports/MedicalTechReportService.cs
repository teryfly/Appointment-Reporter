using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Fhir;

namespace Services.Reports
{
    public class MedicalTechReportService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IOrganizationService _orgService;
        private readonly FhirMedicalTechAggregationService _fhirAgg;

        public MedicalTechReportService(
            IAppointmentRepository repository,
            IOrganizationService orgService,
            FhirMedicalTechAggregationService fhirAgg)
        {
            _repository = repository;
            _orgService = orgService;
            _fhirAgg = fhirAgg;
        }

        public async Task<MedicalTechReportResponse> GetMedicalTechAppointmentsAsync(MedicalTechReportRequest request)
        {
            var performerOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var rows = await _fhirAgg.QueryAppointmentsAsync(
                request.StartDate,
                request.EndDate,
                request.GroupBy,
                performerOrgIds
            );

            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var orgNameMap = orgs.ToDictionary(o => o.Id, o => o.Name);

            var wantedCategories = request.ExamTypes?.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet()
                                   ?? new HashSet<string>();

            var items = new List<MedicalTechReportItem>();
            foreach (var r in rows)
            {
                if (wantedCategories.Count > 0 && !wantedCategories.Contains(r.CategoryDisplay))
                    continue;

                items.Add(new MedicalTechReportItem
                {
                    Slot = r.Slot, // 新增时段字段：按 groupBy 返回 yyyy / yyyy-MM / yyyy-MM-dd
                    OrgId = r.DepartmentId,
                    OrgName = orgNameMap.TryGetValue(r.DepartmentId, out var n) ? n : "",
                    ExamType = r.CategoryDisplay, // 检查类型使用 display
                    AppointmentCount = r.Count,
                    CompletedCount = 0,
                    CancelledCount = 0
                });
            }

            return new MedicalTechReportResponse
            {
                Success = true,
                Data = items,
                Total = items.Count,
                Message = "查询成功"
            };
        }

        public async Task<MedicalTechSourceReportResponse> GetMedicalTechSourcesAsync(MedicalTechSourceReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;
            var appointments = await _repository.GetAppointmentsAsync(
                request.StartDate, request.EndDate, execOrgIds, 2, request.ApplyOrgIds);

            var sources = request.SourceTypes;

            var filtered = appointments
                .Where(a => string.IsNullOrEmpty(a.Scene) || (sources == null || sources.Contains(a.Scene)))
                .ToList();

            var result = filtered
                .GroupBy(a => a.Scene ?? "")
                .Select(g => new MedicalTechSourceReportItem
                {
                    SourceType = g.Key,
                    AppointmentCount = g.Count(a => a.Status != "已取消")
                }).ToList();

            return new MedicalTechSourceReportResponse
            {
                Success = true,
                Data = result,
                Total = result.Count,
                Message = "查询成功"
            };
        }

        public async Task<MedicalTechItemResponse> GetMedicalTechItemsV2Async(MedicalTechItemDetailRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var appointments = await _repository.GetAppointmentsAsync(
                request.StartDate, request.EndDate, execOrgIds, 2, request.ApplyOrgIds);

            var response = new MedicalTechItemResponse
            {
                Success = true,
                Data = new List<MedicalTechItemRow>(),
                Total = 0,
                Message = "查询成功"
            };

            if (appointments.Count == 0)
                return response;

            var appointmentIds = appointments.Select(a => a.Id).Distinct().ToList();
            if (appointmentIds.Count == 0)
                return response;

            var items = await _repository.GetAppointmentItemsByIdsAsync(appointmentIds);

            var itemCodes = request.ItemCodes?.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            if (itemCodes != null && itemCodes.Count > 0)
            {
                items = items.Where(i => i.Code != null && itemCodes.Contains(i.Code)).ToList();
            }

            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var orgNameDict = orgs.ToDictionary(o => o.Id, o => o.Name);

            var apptLookup = appointments.ToDictionary(a => a.Id, a => a);

            string GroupKey(DateTime dt, string groupBy) =>
                groupBy == "year" ? dt.Year.ToString()
                : groupBy == "month" ? $"{dt.Year}-{dt.Month:D2}"
                : dt.ToString("yyyy-MM-dd");

            static int CategoryIndex(string? scene)
            {
                return scene switch
                {
                    "01" => 0,
                    "02" => 1,
                    "03" => 2,
                    _ => -1
                };
            }

            var expanded = new List<(string Date, string OrgId, string OrgName, string ItemCode, string ItemName, int CatIndex)>();
            foreach (var it in items)
            {
                if (!apptLookup.TryGetValue(it.AppointmentId, out var appt))
                    continue;

                var idx = CategoryIndex(appt.Scene);
                if (idx == -1) continue;

                var dateKey = GroupKey(appt.CreateTime, request.GroupBy);
                var orgId = appt.OrgId ?? "";
                var orgName = orgNameDict.TryGetValue(orgId, out var on) ? on : "";

                expanded.Add((dateKey, orgId, orgName, it.Code ?? "", it.Name ?? "", idx));
            }

            var grouped = expanded
                .GroupBy(x => new { x.Date, x.OrgId, x.OrgName, x.ItemCode, x.ItemName })
                .Select(g =>
                {
                    var counts = new int[3];
                    foreach (var r in g)
                        counts[r.CatIndex]++;

                    return new MedicalTechItemRow
                    {
                        Date = g.Key.Date,
                        OrgId = g.Key.OrgId,
                        OrgName = g.Key.OrgName,
                        ItemCode = g.Key.ItemCode,
                        ItemName = g.Key.ItemName,
                        OutpatientCount = counts[0],
                        InpatientCount = counts[1],
                        PhysicalExamCount = counts[2],
                        TotalCount = counts[0] + counts[1] + counts[2]
                    };
                })
                .OrderBy(r => r.Date)
                .ThenBy(r => r.OrgId)
                .ThenBy(r => r.ItemCode)
                .ToList();

            var summary = new MedicalTechItemSummary
            {
                OutpatientTotal = grouped.Sum(x => x.OutpatientCount),
                InpatientTotal = grouped.Sum(x => x.InpatientCount),
                PhysicalExamTotal = grouped.Sum(x => x.PhysicalExamCount),
                GrandTotal = grouped.Sum(x => x.TotalCount)
            };

            response.Data = grouped;
            response.Total = grouped.Count;
            response.Summary = summary;
            return response;
        }
    }
}