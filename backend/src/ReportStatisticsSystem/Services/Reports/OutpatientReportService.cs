using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models.Requests;
using Models.Responses;
using Repositories.Interfaces;
using Services.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Services.Reports
{
    public class OutpatientReportService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IFhirLookupService _fhirLookup;
        private readonly ReportDbContext _db;

        public OutpatientReportService(
            IAppointmentRepository repository,
            IFhirLookupService fhirLookup,
            ReportDbContext db)
        {
            _repository = repository;
            _fhirLookup = fhirLookup;
            _db = db;
        }

        private static string FormatDateKey(DateTime dt, string groupBy)
        {
            return groupBy == "year"
                ? dt.Year.ToString()
                : groupBy == "month"
                    ? $"{dt.Year}-{dt.Month:D2}"
                    : dt.ToString("yyyy-MM-dd");
        }

        public async Task<OutpatientReportResponse> GetOutpatientAppointmentsAsync(OutpatientReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var start = request.StartDate.Date;
            var end = request.EndDate.Date.AddDays(1).AddTicks(-1);

            IQueryable<Models.Entities.NumberProviderEntity> npQuery = _db.NumberProviders.AsNoTracking()
                .Where(n => n.OccurTime >= start && n.OccurTime <= end)
                .Where(n => n.Scene == "01")
                .Where(n => !string.IsNullOrWhiteSpace(n.OrgId) && !string.IsNullOrWhiteSpace(n.DoctorId));

            // 修复 EF Core IN 子句兼容性：直接 Where.Contains
            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var execSet = execOrgIds.Distinct().ToList();
                npQuery = npQuery.Where(n => execSet.Contains(n.OrgId));
            }

            // Merge AM/PM by day
            var slotsDaily = await npQuery
                .GroupBy(n => new { n.OrgId, n.DoctorId, Day = n.OccurTime.Date })
                .Select(g => new
                {
                    g.Key.OrgId,
                    g.Key.DoctorId,
                    g.Key.Day,
                    Count = g.Sum(x => x.NumberCount)
                })
                .ToListAsync();

            // Map to output grain
            var slotByKey = slotsDaily
                .GroupBy(x => new { x.OrgId, x.DoctorId, DateKey = FormatDateKey(x.Day, request.GroupBy) })
                .ToDictionary(k => (k.Key.OrgId, k.Key.DoctorId, k.Key.DateKey),
                              v => v.Sum(x => x.Count));

            // Appointment counts by Requested_Start and Scene == "01"
            var appointments = await _repository.GetAppointmentsAsync(
                request.StartDate, request.EndDate, execOrgIds, 1, request.ApplyOrgIds);

            var apptByKey = appointments
                .Where(a => a.Scene == "01" && a.RequestedStart.HasValue && !string.IsNullOrWhiteSpace(a.OrgId))
                .GroupBy(a => new
                {
                    a.OrgId,
                    DoctorId = a.ResourceResourceId ?? string.Empty,
                    DateKey = FormatDateKey(a.RequestedStart!.Value, request.GroupBy)
                })
                .ToDictionary(g => (g.Key.OrgId, g.Key.DoctorId, g.Key.DateKey),
                              g => g.Count());

            // Union all keys from slot table (authoritative org/doctor set) and appointments
            var allKeys = new HashSet<(string OrgId, string DoctorId, string DateKey)>(slotByKey.Keys);
            foreach (var k in apptByKey.Keys) allKeys.Add(k);

            // Prepare FHIR names for orgs and practitioners based on keys from number_provider
            var allOrgIds = allKeys.Select(k => k.OrgId).Distinct().ToList();
            var allDoctorIds = allKeys.Select(k => k.DoctorId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

            var orgNameMap = await _fhirLookup.GetOrganizationNamesAsync(allOrgIds);
            var doctorNameMap = await _fhirLookup.GetPractitionerNamesAsync(allDoctorIds);

            // Build results
            var results = new List<OutpatientReportItem>();
            foreach (var key in allKeys.OrderBy(k => k.DateKey).ThenBy(k => k.OrgId).ThenBy(k => k.DoctorId))
            {
                var slotCount = slotByKey.TryGetValue(key, out var sc) ? sc : 0;
                var appointmentCount = apptByKey.TryGetValue(key, out var ac) ? ac : 0;

                var orgName = orgNameMap.TryGetValue(key.OrgId, out var on) ? on : "";
                var doctorName = doctorNameMap.TryGetValue(key.DoctorId, out var dn) ? dn : "";

                var personnel = (slotCount > 0 || appointmentCount > 0) ? 1 : 0;

                results.Add(new OutpatientReportItem
                {
                    Date = key.DateKey,
                    OrgId = key.OrgId,
                    OrgName = orgName,
                    PersonnelCount = personnel,
                    SlotCount = slotCount,
                    AppointmentCount = appointmentCount,
                    TotalCount = appointmentCount,
                    DoctorId = key.DoctorId,
                    DoctorName = doctorName
                });
            }

            return new OutpatientReportResponse
            {
                Success = true,
                Data = results,
                Total = results.Count,
                Message = "查询成功"
            };
        }

        public async Task<OutpatientReportResponse> GetAppointmentTimeDistributionAsync(AppointmentTimeDistributionRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;
            var appointments = await _repository.GetAppointmentsAsync(request.StartDate, request.EndDate, execOrgIds, 1, request.ApplyOrgIds);

            var interval = request.TimeInterval;
            string TimeSlot(DateTime dt) =>
                interval == "hour" ? dt.ToString("HH:00") : (dt.Minute < 30 ? dt.ToString("HH:00") : dt.ToString("HH:30"));

            var result = appointments
                .Where(a => a.SlotStart.HasValue && !string.IsNullOrWhiteSpace(a.ResourceResourceId))
                .GroupBy(a => new
                {
                    OrgId = a.OrgId,
                    DoctorId = a.ResourceResourceId!,
                    DoctorName = a.ResourceResourceName ?? "",
                    TimeSlot = TimeSlot(a.SlotStart!.Value)
                })
                .Select(g => new OutpatientReportItem
                {
                    OrgId = g.Key.OrgId,
                    OrgName = "",
                    Date = g.Key.TimeSlot,
                    PersonnelCount = 1,
                    SlotCount = g.Count(),
                    AppointmentCount = g.Count(a => a.Status == "Finish"),
                    TotalCount = g.Count(),
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.DoctorName
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