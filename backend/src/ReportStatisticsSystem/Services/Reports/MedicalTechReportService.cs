using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Entities;
using Models.Requests;
using Models.Responses;
using Repositories.Interfaces;
using Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        public async Task<MedicalTechItemDetailResponse> GetMedicalTechItemsAsync(MedicalTechItemDetailRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;
            var appointments = await _repository.GetAppointmentsAsync(
                request.StartDate, request.EndDate, execOrgIds, 2, request.ApplyOrgIds);

            var appointmentIds = appointments.Select(a => a.Id).Distinct().ToList();
            if (appointmentIds.Count == 0)
            {
                return new MedicalTechItemDetailResponse
                {
                    Success = true,
                    Data = new List<MedicalTechItemDetailItem>(),
                    Total = 0,
                    Message = "查询成功"
                };
            }

            var items = await _repository.GetAppointmentItemsByIdsAsync(appointmentIds);
            var itemCodes = request.ItemCodes?.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            if (itemCodes != null && itemCodes.Count > 0)
            {
                items = items.Where(i => i.Code != null && itemCodes.Contains(i.Code)).ToList();
            }

            // 将预约与明细 Join，取预约创建时间用于时间维度分组
            var apptLookup = appointments.ToDictionary(a => a.Id, a => a.CreateTime);

            string GroupKey(DateTime dt, string groupBy) =>
                groupBy == "year" ? dt.Year.ToString()
                : groupBy == "month" ? $"{dt.Year}-{dt.Month:D2}"
                : dt.ToString("yyyy-MM-dd");

            var grouped = items
                .Select(i =>
                {
                    var dt = apptLookup.TryGetValue(i.AppointmentId, out var ct) ? ct : DateTime.MinValue;
                    return new
                    {
                        DateKey = GroupKey(dt, request.GroupBy),
                        i.Code,
                        i.Name
                    };
                })
                .GroupBy(x => new { x.DateKey, x.Code, x.Name })
                .Select(g => new MedicalTechItemDetailItem
                {
                    DateKey = g.Key.DateKey,
                    ItemCode = g.Key.Code ?? "",
                    ItemName = g.Key.Name ?? "",
                    AppointmentCount = g.Count(),
                    CompletedCount = g.Count()
                })
                .OrderBy(r => r.DateKey)
                .ThenBy(r => r.ItemCode)
                .ToList();

            return new MedicalTechItemDetailResponse
            {
                Success = true,
                Data = grouped,
                Total = grouped.Count,
                Message = "查询成功"
            };
        }
    }
}