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
        private readonly FhirServiceRequestSourceAggregationService _fhirSourceAgg;

        public MedicalTechReportService(
            IAppointmentRepository repository,
            IOrganizationService orgService,
            FhirMedicalTechAggregationService fhirAgg,
            FhirServiceRequestSourceAggregationService fhirSourceAgg)
        {
            _repository = repository;
            _orgService = orgService;
            _fhirAgg = fhirAgg;
            _fhirSourceAgg = fhirSourceAgg;
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
                    Slot = r.Slot,
                    OrgId = r.DepartmentId,
                    OrgName = orgNameMap.TryGetValue(r.DepartmentId, out var n) ? n : "",
                    ExamType = r.CategoryDisplay,
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

        // New spec: return flattened rows [{ orgId, orgName, slot, outpatientCount, inpatientCount, physicalExamCount, totalCount }]
        public async Task<MedicalTechSourceReportResponse> GetMedicalTechSourcesAsync(MedicalTechSourceReportRequest request)
        {
            var performerOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var rows = await _fhirSourceAgg.AggregateRowsAsync(
                request.StartDate,
                request.EndDate,
                request.GroupBy,
                performerOrgIds
            );

            // resolve org names via OrganizationService (scene=02)
            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var orgNameMap = orgs.ToDictionary(o => o.Id, o => o.Name);

            var data = rows.Select(r => new MedicalTechSourceFlatItem
            {
                OrgId = r.OrgId,
                OrgName = orgNameMap.TryGetValue(r.OrgId, out var n) ? n : "",
                Slot = r.Slot,
                OutpatientCount = r.Outpatient,
                InpatientCount = r.Inpatient,
                PhysicalExamCount = r.PhysicalExam,
                TotalCount = r.Outpatient + r.Inpatient + r.PhysicalExam
            })
            .OrderBy(x => x.OrgId)
            .ThenBy(x => x.Slot)
            .ToList();

            return new MedicalTechSourceReportResponse
            {
                Success = true,
                Data = data,
                Total = data.Count,
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