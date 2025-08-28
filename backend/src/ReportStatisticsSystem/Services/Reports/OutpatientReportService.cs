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
using Models.Entities;

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

        // Helpers
        private static bool IsValidAppointmentStatusHelper(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return true;
            var val = s.Trim();
            return !(val.Equals("Cancel", StringComparison.OrdinalIgnoreCase)
                  || val.Equals("Alternate", StringComparison.OrdinalIgnoreCase)
                  || val.Equals("AlternateFailed", StringComparison.OrdinalIgnoreCase)
                  || val.Equals("Draft", StringComparison.OrdinalIgnoreCase)
                  || val.Equals("已取消", StringComparison.OrdinalIgnoreCase));
        }
        private static string HourBucketHelper(DateTime dt) => dt.ToString("yyyy-MM-dd HH:00:00");

        // Restored: original endpoint logic for outpatient appointments report
        public async Task<OutpatientReportResponse> GetOutpatientAppointmentsAsync(OutpatientReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            // 统计时间范围（按天/月/年聚合时用于键格式，不改变查询范围）
            var start = request.StartDate.Date;
            var end = request.EndDate.Date.AddDays(1).AddTicks(-1);

            // 放号量：number_provider（Scene=01）
            var numberProvidersQuery = _db.NumberProviders.AsNoTracking()
                .Where(np => np.Scene == "01")
                .Where(np => np.Start >= start && np.Start <= end)
                .Where(np => !string.IsNullOrEmpty(np.OrgId) && !string.IsNullOrEmpty(np.DoctorId));

            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var orgSet = execOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (orgSet.Count == 1)
                {
                    var orgId = orgSet[0];
                    numberProvidersQuery = numberProvidersQuery.Where(np => np.OrgId == orgId);
                }
                else
                {
                    numberProvidersQuery = numberProvidersQuery.Where(np => orgSet.Contains(np.OrgId));
                }
            }

            var slotsRaw = await numberProvidersQuery
                .Select(np => new
                {
                    np.OrgId,
                    np.DoctorId,
                    StartTime = np.Start,
                    np.NumberCount
                })
                .ToListAsync();

            var slotsGrouped = slotsRaw
                .GroupBy(x => new
                {
                    x.OrgId,
                    x.DoctorId,
                    DateKey = FormatDateKey(x.StartTime, request.GroupBy)
                })
                .Select(g => new
                {
                    g.Key.OrgId,
                    g.Key.DoctorId,
                    g.Key.DateKey,
                    Count = g.Sum(x => x.NumberCount)
                })
                .ToList();

            var slotByKey = slotsGrouped.ToDictionary(k => (k.OrgId, k.DoctorId, k.DateKey), v => v.Count);

            // 预约量：appointment（AppointmentType=1, Scene='01'，按 RequestedStart）
            var appointmentsQuery = _db.Appointments.AsNoTracking()
                .Where(a => a.AppointmentType == 1 && a.Scene == "01")
                .Where(a => a.RequestedStart.HasValue && a.RequestedStart.Value >= start && a.RequestedStart.Value <= end)
                .Where(a => !string.IsNullOrWhiteSpace(a.OrgId));

            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var orgSet = execOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                appointmentsQuery = orgSet.Count == 1
                    ? appointmentsQuery.Where(a => a.OrgId == orgSet[0])
                    : appointmentsQuery.Where(a => orgSet.Contains(a.OrgId));
            }

            var applyOrgIds = request.ApplyOrgIds;
            if (applyOrgIds != null && applyOrgIds.Count > 0)
            {
                var applySet = applyOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                appointmentsQuery = applySet.Count == 1
                    ? appointmentsQuery.Where(a => a.ApplyOrgId == applySet[0])
                    : appointmentsQuery.Where(a => a.ApplyOrgId != null && applySet.Contains(a.ApplyOrgId!));
            }

            var appointments = await appointmentsQuery
                .Select(a => new
                {
                    a.OrgId,
                    DoctorId = a.ResourceResourceId ?? "",
                    DoctorName = a.ResourceResourceName ?? "",
                    a.RequestedStart,
                    a.Status
                })
                .ToListAsync();

            var apptByKey = appointments
                .Where(a => a.RequestedStart.HasValue && IsValidAppointmentStatusHelper(a.Status))
                .GroupBy(a => new
                {
                    a.OrgId,
                    a.DoctorId,
                    DateKey = FormatDateKey(a.RequestedStart!.Value, request.GroupBy)
                })
                .ToDictionary(
                    g => (g.Key.OrgId, g.Key.DoctorId, g.Key.DateKey),
                    g => g.Count()
                );

            // 合并键：OrgId + DoctorId + DateKey
            var keys = new HashSet<(string OrgId, string DoctorId, string DateKey)>(slotByKey.Keys);
            foreach (var k in apptByKey.Keys) keys.Add(k);

            // 名称映射
            var allOrgIds = keys.Select(k => k.OrgId).Distinct().ToList();
            var allDoctorIds = keys.Select(k => k.DoctorId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

            var orgNameMap = await _fhirLookup.GetOrganizationNamesAsync(allOrgIds);
            var doctorNameMap = await _fhirLookup.GetPractitionerNamesAsync(allDoctorIds);

            var results = new List<OutpatientReportItem>();
            foreach (var k in keys.OrderBy(k => k.DateKey).ThenBy(k => k.OrgId).ThenBy(k => k.DoctorId))
            {
                var slotCount = slotByKey.TryGetValue(k, out var sc) ? sc : 0;
                var apptCount = apptByKey.TryGetValue(k, out var ac) ? ac : 0;

                var orgName = orgNameMap.TryGetValue(k.OrgId, out var on) ? on : "";
                var doctorName = doctorNameMap.TryGetValue(k.DoctorId, out var dn) ? dn : "";

                var personnel = (slotCount > 0 || apptCount > 0) ? 1 : 0;

                results.Add(new OutpatientReportItem
                {
                    Date = k.DateKey,
                    OrgId = k.OrgId,
                    OrgName = orgName,
                    PersonnelCount = personnel,
                    SlotCount = slotCount,
                    AppointmentCount = apptCount,
                    TotalCount = apptCount, // 与原服务保持一致
                    DoctorId = k.DoctorId,
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

        // New: appointment-time-distribution combined from SQL-1 and SQL-2
        public async Task<OutpatientReportResponse> GetAppointmentTimeDistributionAsync(AppointmentTimeDistributionRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            var start = request.StartDate.Date;
            var end = request.EndDate.Date.AddDays(1);

            // SQL-1：预约量
            var apptQuery = _db.Appointments.AsNoTracking()
                .Where(a => a.Scene == "01")
                .Where(a => a.RequestedStart.HasValue && a.RequestedStart.Value >= start && a.RequestedStart.Value < end);

            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var orgSet = execOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                apptQuery = orgSet.Count == 1
                    ? apptQuery.Where(a => a.OrgId == orgSet[0])
                    : apptQuery.Where(a => orgSet.Contains(a.OrgId));
            }

            var apptList = await apptQuery
                .Select(a => new
                {
                    a.OrgId,
                    Doctor = a.ResourceResourceName ?? "",
                    Hour = HourBucketHelper(a.RequestedStart!.Value),
                    a.Status
                })
                .ToListAsync();

            var apptByKey = apptList
                .Where(a => IsValidAppointmentStatusHelper(a.Status))
                .GroupBy(a => new { a.OrgId, a.Doctor, a.Hour })
                .ToDictionary(g => (g.Key.OrgId, g.Key.Doctor, g.Key.Hour), g => g.Count());

            // SQL-2：就诊量（跨库）
            string orgFilterSql;
            var sqlParams = new List<object> { start, end };

            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var valid = execOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (valid.Count == 1)
                {
                    orgFilterSql = " AND [qr].[itemCode] = {2}";
                    sqlParams.Add(valid[0]!);
                }
                else
                {
                    var inParams = new List<string>();
                    for (int i = 0; i < valid.Count; i++)
                    {
                        inParams.Add($"{{{i + 2}}}");
                        sqlParams.Add(valid[i]!);
                    }
                    orgFilterSql = $" AND [qr].[itemCode] IN ({string.Join(",", inParams)})";
                }
            }
            else
            {
                orgFilterSql = string.Empty;
            }

            var sql = $@"
SELECT
  COALESCE([qr].[itemCode], N'') AS [ItemCode],
  COALESCE([qr].[itemName], N'') AS [ItemName],
  COALESCE([qr].[doctorname], N'') AS [DoctorName],
  [qr].[appointmentSourceStartTime] AS [AppointmentSourceStartTime]
FROM [isp-scheduler].[schedule].[QueueRecords] AS [qr]
WHERE [qr].[Status] = N'5' AND [qr].[IsDeleted] = 0 AND [qr].[QueueType] = N'01'
  AND [qr].[appointmentSourceStartTime] >= {{0}} AND [qr].[appointmentSourceStartTime] < {{1}}
  {orgFilterSql}";

            var rawVisits = await _db.QueueRecordReadModels
                .FromSqlRaw(sql, sqlParams.ToArray())
                .AsNoTracking()
                .Select(q => new
                {
                    OrgId = q.ItemCode ?? "",
                    OrgName = q.ItemName ?? "",
                    Doctor = q.DoctorName ?? "",
                    Hour = HourBucketHelper(q.AppointmentSourceStartTime)
                })
                .ToListAsync();

            var visitByKey = rawVisits
                .GroupBy(v => new { v.OrgId, v.OrgName, v.Doctor, v.Hour })
                .ToDictionary(g => (g.Key.OrgId, g.Key.OrgName, g.Key.Doctor, g.Key.Hour), g => g.Count());

            // 合并键
            var keys = new HashSet<(string OrgId, string Doctor, string Hour)>(apptByKey.Keys);
            foreach (var k in visitByKey.Keys)
                keys.Add((k.OrgId, k.Doctor, k.Hour));

            // 组织名称映射
            var orgIds = keys.Select(k => k.OrgId).Distinct().ToList();
            var orgNameMap = await _fhirLookup.GetOrganizationNamesAsync(orgIds);

            var result = new List<OutpatientReportItem>();
            foreach (var k in keys.OrderBy(x => x.Hour).ThenBy(x => x.OrgId).ThenBy(x => x.Doctor))
            {
                var apptCount = apptByKey.TryGetValue((k.OrgId, k.Doctor, k.Hour), out var aCnt) ? aCnt : 0;

                var orgNameFromVisit = visitByKey.Keys
                    .Where(vk => vk.OrgId == k.OrgId && vk.Doctor == k.Doctor && vk.Hour == k.Hour)
                    .Select(vk => vk.OrgName)
                    .FirstOrDefault();

                var visitCount = visitByKey.TryGetValue((k.OrgId, orgNameFromVisit ?? "", k.Doctor, k.Hour), out var vCnt)
                    ? vCnt
                    : 0;

                var orgName = !string.IsNullOrWhiteSpace(orgNameFromVisit)
                    ? orgNameFromVisit!
                    : (orgNameMap.TryGetValue(k.OrgId, out var n) ? n : "");

                // 计算率（可由前端展示）
                // 预约就诊率 = visit/appointment * 100%; 爽约率 = (1 - visit/appointment) * 100%
                // 若需要直接返回，可扩展响应模型。

                result.Add(new OutpatientReportItem
                {
                    OrgId = k.OrgId,
                    OrgName = orgName,
                    Date = k.Hour,
                    PersonnelCount = 1,
                    SlotCount = 0,
                    AppointmentCount = apptCount, // 预约量
                    TotalCount = visitCount,      // 就诊量
                    DoctorId = "",
                    DoctorName = k.Doctor
                });
            }

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