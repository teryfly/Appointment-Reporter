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
    public class OutpatientReportService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IOrganizationService _orgService;
        private readonly IPatientService _patientService;

        public OutpatientReportService(
            IAppointmentRepository repository,
            IOrganizationService orgService,
            IPatientService patientService)
        {
            _repository = repository;
            _orgService = orgService;
            _patientService = patientService;
        }

        public async Task<OutpatientReportResponse> GetOutpatientAppointmentsAsync(OutpatientReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            // 第一次：只查数据库，构建不含 PatientName 的结果集
            var appointments = await _repository.GetAppointmentsAsync(
                request.StartDate, request.EndDate, execOrgIds, 1, request.ApplyOrgIds);

            var orgs = await _orgService.GetOrganizationsBySceneAsync("01");
            var orgNameDict = orgs.ToDictionary(o => o.Id, o => o.Name);

            string DateKey(DateTime dt) =>
                request.GroupBy == "year" ? dt.Year.ToString()
                : request.GroupBy == "month" ? $"{dt.Year}-{dt.Month:D2}"
                : dt.ToString("yyyy-MM-dd");

            var interim = appointments
                .GroupBy(a => new { OrgId = a.OrgId, Date = DateKey(a.CreateTime) })
                .Select(g =>
                {
                    // 仅确定 PatientId（组内存在多个患者时，取出现次数最多的那个）
                    var patientId = g.Where(x => !string.IsNullOrWhiteSpace(x.PatientId))
                                     .GroupBy(x => x.PatientId!)
                                     .OrderByDescending(gg => gg.Count())
                                     .ThenBy(gg => gg.Key)
                                     .Select(gg => gg.Key)
                                     .FirstOrDefault() ?? string.Empty;

                    return new OutpatientReportItem
                    {
                        Date = g.Key.Date,
                        OrgId = g.Key.OrgId,
                        OrgName = orgNameDict.TryGetValue(g.Key.OrgId, out var orgName) ? orgName : "",
                        PersonnelCount = g.Select(a => a.ResourceResourceId)
                                          .Where(id => !string.IsNullOrWhiteSpace(id))
                                          .Distinct()
                                          .Count(),
                        SlotCount = g.Count(),
                        AppointmentCount = g.Count(a => a.Status != "已取消"),
                        TotalCount = g.Count(),
                        PatientId = patientId,
                        PatientName = "未知" // 暂不填充
                    };
                })
                .OrderBy(r => r.Date)
                .ThenBy(r => r.OrgId)
                .ToList();

            // 第二次：遍历第一次结果中的 PatientId 去查 FHIR，并回填 PatientName
            var distinctPatientIds = interim
                .Select(r => r.PatientId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var nameMap = new Dictionary<string, string>();
            foreach (var pid in distinctPatientIds)
            {
                var name = await _patientService.GetPatientNameAsync(pid);
                nameMap[pid] = string.IsNullOrWhiteSpace(name) ? "未知" : name;
            }

            foreach (var row in interim)
            {
                if (!string.IsNullOrWhiteSpace(row.PatientId) && nameMap.TryGetValue(row.PatientId, out var nm))
                    row.PatientName = nm;
                else
                    row.PatientName = "未知";
            }

            return new OutpatientReportResponse
            {
                Success = true,
                Data = interim,
                Total = interim.Count,
                Message = "查询成功"
            };
        }

        public async Task<OutpatientReportResponse> GetAppointmentTimeDistributionAsync(AppointmentTimeDistributionRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;
            var appointments = await _repository.GetAppointmentsAsync(request.StartDate, request.EndDate, execOrgIds, 1, request.ApplyOrgIds);
            var orgs = await _orgService.GetOrganizationsBySceneAsync("01");
            var interval = request.TimeInterval;

            string TimeSlot(DateTime dt) =>
                interval == "hour"
                    ? dt.ToString("HH:00")
                    : (dt.Minute < 30 ? dt.ToString("HH:00") : dt.ToString("HH:30"));

            var result = appointments
                .Where(a => a.SlotStart.HasValue && !string.IsNullOrWhiteSpace(a.ResourceResourceId))
                .GroupBy(a => new
                {
                    OrgId = a.OrgId,
                    DoctorId = a.ResourceResourceId!,
                    DoctorName = a.ResourceResourceName ?? "",
                    TimeSlot = TimeSlot(a.SlotStart!.Value)
                })
                .Select(g =>
                {
                    var org = orgs.FirstOrDefault(o => o.Id == g.Key.OrgId);
                    return new OutpatientReportItem
                    {
                        OrgId = g.Key.OrgId,
                        OrgName = org?.Name ?? "",
                        Date = g.Key.TimeSlot,
                        PersonnelCount = 1,
                        SlotCount = g.Count(),
                        AppointmentCount = g.Count(a => a.Status != "已取消"),
                        TotalCount = g.Count(),
                        PatientId = string.Empty,
                        PatientName = "未知"
                    };
                })
                .OrderBy(r => r.OrgId)
                .ThenBy(r => r.Date)
                .ToList();

            return new OutpatientReportResponse
            {
                Success = true,
                Data = result,
                Total = result.Count,
                Message = "查询成功"
            };
        }
    }
}