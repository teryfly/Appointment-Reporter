using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;
using Repositories.Interfaces;

namespace Services.Reports
{
    public class DoctorAnalysisReportService
    {
        private readonly IAppointmentRepository _repository;

        public DoctorAnalysisReportService(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        private static string BuildDateKey(DateTime dt, string groupBy)
        {
            return groupBy == "year"
                ? dt.Year.ToString()
                : groupBy == "month"
                    ? $"{dt.Year}-{dt.Month:D2}"
                    : dt.ToString("yyyy-MM-dd");
        }

        public async Task<DoctorAnalysisResponse> GetDoctorAppointmentAnalysisAsync(DoctorAnalysisReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var appointments = await _repository.GetAppointmentsAsync(
                request.StartDate, request.EndDate, execOrgIds, null, request.ApplyOrgIds);

            // 仅保留有医生ID的记录（appointment.Resource_ResourceId）
            var filtered = appointments.Where(a => !string.IsNullOrWhiteSpace(a.ResourceResourceId));

            // 指定医生过滤
            if (request.DoctorIds != null && request.DoctorIds.Count > 0)
            {
                var idsSet = request.DoctorIds.ToHashSet();
                filtered = filtered.Where(a => a.ResourceResourceId != null && idsSet.Contains(a.ResourceResourceId));
            }

            var grouped = filtered
                .GroupBy(a => new
                {
                    DateKey = BuildDateKey(a.CreateTime, request.GroupBy),
                    DoctorId = a.ResourceResourceId ?? "",
                    DoctorName = a.ResourceResourceName ?? ""
                })
                .Select(g => new DoctorAnalysisItem
                {
                    Date = g.Key.DateKey,
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.DoctorName,
                    OrdersCount = g.Count(),
                    AppointmentCount = g.Count(a => a.Status != "已取消"),
                    AppointmentRate = g.Count() > 0
                        ? Math.Round((double)g.Count(a => a.Status != "已取消") / g.Count() * 100, 2)
                        : 0
                })
                .OrderBy(r => r.Date)
                .ThenBy(r => r.DoctorName)
                .ToList();

            return new DoctorAnalysisResponse
            {
                Success = true,
                Data = grouped,
                Total = grouped.Count,
                Message = "查询成功"
            };
        }
    }
}