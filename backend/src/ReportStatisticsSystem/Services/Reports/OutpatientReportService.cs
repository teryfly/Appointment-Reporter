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

        public OutpatientReportService(
            IAppointmentRepository repository,
            IOrganizationService orgService)
        {
            _repository = repository;
            _orgService = orgService;
        }

        public async Task<OutpatientReportResponse> GetOutpatientAppointmentsAsync(OutpatientReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;
            var appointments = await _repository.GetAppointmentsAsync(request.StartDate, request.EndDate, execOrgIds, 1, request.ApplyOrgIds);
            var orgs = await _orgService.GetOrganizationsBySceneAsync("01");
            var groupBy = request.GroupBy;

            var result = appointments
                .GroupBy(a => new
                {
                    OrgId = a.OrgId,
                    Date = groupBy == "year" ? a.CreateTime.Year.ToString()
                        : groupBy == "month" ? $"{a.CreateTime.Year}-{a.CreateTime.Month:D2}"
                        : a.CreateTime.ToString("yyyy-MM-dd")
                })
                .Select(g =>
                {
                    var org = orgs.FirstOrDefault(o => o.Id == g.Key.OrgId);
                    return new OutpatientReportItem
                    {
                        OrgId = g.Key.OrgId,
                        OrgName = org?.Name ?? "",
                        Date = g.Key.Date,
                        PersonnelCount = g.Select(a => a.ResourceResourceId).Distinct().Count(),
                        SlotCount = g.Count(),
                        AppointmentCount = g.Count(a => a.Status != "已取消"),
                        CompletedCount = g.Count(a => a.Status == "已完成"),
                        CancelledCount = g.Count(a => a.Status == "已取消"),
                        DoctorId = null
                    };
                }).ToList();

            return new OutpatientReportResponse
            {
                Success = true,
                Data = result,
                Total = result.Count,
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
                    DoctorId = a.ResourceResourceId!, // 非空已在 Where 中保证
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
                        PersonnelCount = 1, // 单医生分组下人员=1
                        SlotCount = g.Count(),
                        AppointmentCount = g.Count(a => a.Status != "已取消"),
                        CompletedCount = g.Count(a => a.Status == "已完成"),
                        CancelledCount = g.Count(a => a.Status == "已取消"),
                        DoctorId = g.Key.DoctorId
                    };
                })
                .OrderBy(r => r.OrgId)
                .ThenBy(r => r.DoctorId)
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