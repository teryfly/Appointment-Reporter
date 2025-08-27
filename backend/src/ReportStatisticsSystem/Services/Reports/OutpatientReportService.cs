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

        public async Task<OutpatientReportResponse> GetOutpatientAppointmentsAsync(OutpatientReportRequest request)
        {
            var execOrgIds = request.ExecOrgIds ?? request.OrgIds;

            // 统计口径：
            // 放号量：直接基于 number_provider 表的数据，按 OrgId 和 DoctorId 分组统计
            // 预约量：使用 appointment.RequestedStart 字段
            var start = request.StartDate.Date;
            var end = request.EndDate.Date.AddDays(1).AddTicks(-1);

            // 放号量：直接从 number_provider 获取，严格用 Start 字段（已修正为属性映射）
            var numberProvidersQuery = _db.NumberProviders.AsNoTracking()
                .Where(np => np.Scene == "01")
                .Where(np => np.Start >= start && np.Start <= end)
                .Where(np => !string.IsNullOrEmpty(np.OrgId) && !string.IsNullOrEmpty(np.DoctorId));

            // 科室过滤
            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var orgSet = execOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (orgSet.Count == 1)
                {
                    var orgId = orgSet[0];
                    numberProvidersQuery = numberProvidersQuery.Where(np => np.OrgId == orgId);
                }
                else if (orgSet.Count > 1)
                {
                    numberProvidersQuery = numberProvidersQuery.Where(np => orgSet.Contains(np.OrgId));
                }
            }

            // 获取放号数据
            var slotsRaw = await numberProvidersQuery
                .Select(np => new
                {
                    np.OrgId,
                    np.DoctorId,
                    StartTime = np.Start,
                    np.NumberCount
                })
                .ToListAsync();

            var slotsDaily = slotsRaw
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

            var slotByKey = slotsDaily.ToDictionary(
                k => (k.OrgId, k.DoctorId, k.DateKey),
                v => v.Count);

            // 预约量：门诊 AppointmentType=1，Scene='01'，按 RequestedStart 过滤
            var appointmentsQuery = _db.Appointments.AsNoTracking()
                .Where(a => a.AppointmentType == 1 && a.Scene == "01")
                .Where(a => a.RequestedStart.HasValue && a.RequestedStart.Value >= start && a.RequestedStart.Value <= end)
                .Where(a => !string.IsNullOrWhiteSpace(a.OrgId));

            // 执行科室过滤
            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var orgSet = execOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (orgSet.Count == 1)
                {
                    var orgId = orgSet[0];
                    appointmentsQuery = appointmentsQuery.Where(a => a.OrgId == orgId);
                }
                else if (orgSet.Count > 1)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => orgSet.Contains(a.OrgId));
                }
            }
            // 申请科室过滤
            var applyOrgIds = request.ApplyOrgIds;
            if (applyOrgIds != null && applyOrgIds.Count > 0)
            {
                var applySet = applyOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (applySet.Count == 1)
                {
                    var applyId = applySet[0];
                    appointmentsQuery = appointmentsQuery.Where(a => a.ApplyOrgId == applyId);
                }
                else if (applySet.Count > 1)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.ApplyOrgId != null && applySet.Contains(a.ApplyOrgId));
                }
            }

            // 先拉取数据，再分组
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
                .GroupBy(a => new
                {
                    a.OrgId,
                    a.DoctorId,
                    DateKey = FormatDateKey(a.RequestedStart!.Value, request.GroupBy)
                })
                .ToDictionary(
                    g => (g.Key.OrgId, g.Key.DoctorId, g.Key.DateKey),
                    g => g.Count(appt => appt.Status != "已取消")
                );

            // 合并所有的键（科室+医生+日期组合）
            var allKeys = new HashSet<(string OrgId, string DoctorId, string DateKey)>(slotByKey.Keys);
            foreach (var k in apptByKey.Keys) allKeys.Add(k);

            // 获取 FHIR 中的科室和医生名称
            var allOrgIds = allKeys.Select(k => k.OrgId).Distinct().ToList();
            var allDoctorIds = allKeys.Select(k => k.DoctorId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

            var orgNameMap = await _fhirLookup.GetOrganizationNamesAsync(allOrgIds);
            var doctorNameMap = await _fhirLookup.GetPractitionerNamesAsync(allDoctorIds);

            // 构建结果
            var results = new List<OutpatientReportItem>();
            foreach (var key in allKeys.OrderBy(k => k.DateKey).ThenBy(k => k.OrgId).ThenBy(k => k.DoctorId))
            {
                var slotCount = slotByKey.TryGetValue(key, out var sc) ? sc : 0;
                var appointmentCount = apptByKey.TryGetValue(key, out var ac) ? ac : 0;

                var orgName = orgNameMap.TryGetValue(key.OrgId, out var on) ? on : "";
                var doctorName = doctorNameMap.TryGetValue(key.DoctorId, out var dn) ? dn : "";

                // 人员数量：有放号或预约数据就计为1个医生
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

            var start = request.StartDate.Date;
            var end = request.EndDate.Date.AddDays(1).AddTicks(-1);

            // 使用 RequestedStart 作为统计时间口径，过滤门诊
            var appointmentsQuery = _db.Appointments.AsNoTracking()
                .Where(a => a.AppointmentType == 1 && a.Scene == "01")
                .Where(a => a.RequestedStart.HasValue && a.RequestedStart.Value >= start && a.RequestedStart.Value <= end);

            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var orgSet = execOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (orgSet.Count == 1)
                {
                    var orgId = orgSet[0];
                    appointmentsQuery = appointmentsQuery.Where(a => a.OrgId == orgId);
                }
                else if (orgSet.Count > 1)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => orgSet.Contains(a.OrgId));
                }
            }

            var applyOrgIds = request.ApplyOrgIds;
            if (applyOrgIds != null && applyOrgIds.Count > 0)
            {
                var applySet = applyOrgIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (applySet.Count == 1)
                {
                    var applyId = applySet[0];
                    appointmentsQuery = appointmentsQuery.Where(a => a.ApplyOrgId == applyId);
                }
                else if (applySet.Count > 1)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.ApplyOrgId != null && applySet.Contains(a.ApplyOrgId));
                }
            }

            var appointments = await appointmentsQuery
                .Select(a => new
                {
                    a.OrgId,
                    DoctorId = a.ResourceResourceId ?? "",
                    DoctorName = a.ResourceResourceName ?? "",
                    a.SlotStart,
                    a.Status
                })
                .ToListAsync();

            var interval = request.TimeInterval;
            string TimeSlot(DateTime dt) =>
                interval == "hour" ? dt.ToString("HH:00") : (dt.Minute < 30 ? dt.ToString("HH:00") : dt.ToString("HH:30"));

            var result = appointments
                .Where(a => a.SlotStart.HasValue && !string.IsNullOrWhiteSpace(a.DoctorId))
                .GroupBy(a => new
                {
                    OrgId = a.OrgId,
                    DoctorId = a.DoctorId,
                    DoctorName = a.DoctorName,
                    TimeSlot = TimeSlot(a.SlotStart!.Value)
                })
                .Select(g => new OutpatientReportItem
                {
                    OrgId = g.Key.OrgId,
                    OrgName = "",
                    Date = g.Key.TimeSlot,
                    PersonnelCount = 1,
                    SlotCount = g.Count(),
                    AppointmentCount = g.Count(a => a.Status == "已完成"),
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