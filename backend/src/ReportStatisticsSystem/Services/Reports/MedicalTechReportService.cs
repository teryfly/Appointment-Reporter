using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Reports
{
    public class MedicalTechReportService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IOrganizationService _orgService;

        public MedicalTechReportService(
            IAppointmentRepository repository,
            IOrganizationService orgService)
        {
            _repository = repository;
            _orgService = orgService;
        }

        public async Task<MedicalTechReportResponse> GetMedicalTechAppointmentsAsync(MedicalTechReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;
            var appointments = await _repository.GetAppointmentsAsync(
                request.StartDate, request.EndDate, execOrgIds, 2, request.ApplyOrgIds);

            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var examTypes = request.ExamTypes;

            var filtered = appointments
                .Where(a => string.IsNullOrEmpty(a.ResourceResourceType) || (examTypes == null || examTypes.Contains(a.ResourceResourceType)))
                .ToList();

            var result = filtered
                .GroupBy(a => new { a.ResourceResourceType, a.OrgId })
                .Select(g =>
                {
                    var org = orgs.FirstOrDefault(o => o.Id == g.Key.OrgId);
                    return new MedicalTechReportItem
                    {
                        ExamType = g.Key.ResourceResourceType ?? "",
                        OrgId = g.Key.OrgId,
                        OrgName = org?.Name ?? "",
                        AppointmentCount = g.Count(a => a.Status != "已取消"),
                        CompletedCount = g.Count(a => a.Status == "已完成"),
                        CancelledCount = g.Count(a => a.Status == "已取消")
                    };
                }).ToList();

            return new MedicalTechReportResponse
            {
                Success = true,
                Data = result,
                Total = result.Count,
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

        // 输出包含：日期、科室、检查项目、门诊/住院/体检预约量与合计；忽略其它 Scene 值
        public async Task<MedicalTechItemResponse> GetMedicalTechItemsV2Async(MedicalTechItemDetailRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            // 仅医技预约
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

            // 按项目编码过滤
            var itemCodes = request.ItemCodes?.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            if (itemCodes != null && itemCodes.Count > 0)
            {
                items = items.Where(i => i.Code != null && itemCodes.Contains(i.Code)).ToList();
            }

            // 科室名称
            var orgs = await _orgService.GetOrganizationsBySceneAsync("02");
            var orgNameDict = orgs.ToDictionary(o => o.Id, o => o.Name);

            // 预约查找
            var apptLookup = appointments.ToDictionary(a => a.Id, a => a);

            // 分组键格式
            string GroupKey(DateTime dt, string groupBy) =>
                groupBy == "year" ? dt.Year.ToString()
                : groupBy == "month" ? $"{dt.Year}-{dt.Month:D2}"
                : dt.ToString("yyyy-MM-dd");

            // Scene 到三大分类（忽略其余）
            static int CategoryIndex(string? scene)
            {
                return scene switch
                {
                    "01" => 0, // 门诊
                    "02" => 1, // 住院
                    "03" => 2, // 体检
                    _ => -1    // 其它忽略
                };
            }

            // 展开为基础记录
            var expanded = new List<(string Date, string OrgId, string OrgName, string ItemCode, string ItemName, int CatIndex)>();
            foreach (var it in items)
            {
                if (!apptLookup.TryGetValue(it.AppointmentId, out var appt))
                    continue;

                var idx = CategoryIndex(appt.Scene);
                if (idx == -1) continue; // 忽略其它编码

                var dateKey = GroupKey(appt.CreateTime, request.GroupBy);
                var orgId = appt.OrgId ?? "";
                var orgName = orgNameDict.TryGetValue(orgId, out var on) ? on : "";

                expanded.Add((dateKey, orgId, orgName, it.Code ?? "", it.Name ?? "", idx));
            }

            // 聚合：日期 + 科室 + 项目
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

            // 汇总
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