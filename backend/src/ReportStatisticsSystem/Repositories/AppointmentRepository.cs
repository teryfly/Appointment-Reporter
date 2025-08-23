using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly ReportDbContext _db;
        public AppointmentRepository(ReportDbContext db)
        {
            _db = db;
        }
        public async Task<List<AppointmentEntity>> GetAppointmentsAsync(
            DateTime startDate,
            DateTime endDate,
            List<string>? execOrgIds,
            int? appointmentType = null,
            List<string>? applyOrgIds = null)
        {
            IQueryable<AppointmentEntity> query = _db.Appointments.AsNoTracking()
                .Where(a => a.CreateTime >= startDate && a.CreateTime <= endDate);
            if (appointmentType.HasValue)
            {
                int at = appointmentType.Value;
                query = query.Where(a => a.AppointmentType == at);
            }
            // 使用数据库端子查询进行过滤，避免内联 OR/Union 导致表达式树过大或栈溢出
            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var execOrgIdsQuery = execOrgIds.Distinct().AsQueryable();
                query = from a in query
                        join e in execOrgIdsQuery on a.OrgId equals e
                        select a;
            }
            if (applyOrgIds != null && applyOrgIds.Count > 0)
            {
                var applyOrgIdsQuery = applyOrgIds.Distinct().AsQueryable();
                query = from a in query
                        join ap in applyOrgIdsQuery on a.ApplyOrgId equals ap
                        select a;
            }
            return await query.ToListAsync();
        }
        public async Task<List<AppointmentItemEntity>> GetAppointmentItemsByIdsAsync(List<string> appointmentIds)
        {
            if (appointmentIds == null || appointmentIds.Count == 0)
                return new List<AppointmentItemEntity>();
            var idsQuery = appointmentIds.Distinct().AsQueryable();
            var query = from it in _db.AppointmentItems.AsNoTracking()
                        join id in idsQuery on it.AppointmentId equals id
                        select it;
            return await query.ToListAsync();
        }
        public async Task<List<AppointmentNumberEntity>> GetAppointmentNumbersByIdsAsync(List<string> appointmentIds)
        {
            if (appointmentIds == null || appointmentIds.Count == 0)
                return new List<AppointmentNumberEntity>();
            var idsQuery = appointmentIds.Distinct().AsQueryable();
            var query = from n in _db.AppointmentNumbers.AsNoTracking()
                        join id in idsQuery on n.AppointmentId equals id
                        select n;
            return await query.ToListAsync();
        }
        public async Task<List<AppointmentPropertyEntity>> GetAppointmentPropertiesByIdsAsync(List<string> appointmentIds)
        {
            if (appointmentIds == null || appointmentIds.Count == 0)
                return new List<AppointmentPropertyEntity>();
            var idsQuery = appointmentIds.Distinct().AsQueryable();
            var query = from p in _db.AppointmentProperties.AsNoTracking()
                        join id in idsQuery on p.AppointmentId equals id
                        select p;
            return await query.ToListAsync();
        }
        public async Task<List<AppointmentFailedRecordEntity>> GetAppointmentFailedRecordsAsync(DateTime startDate, DateTime endDate, List<string>? execOrgIds)
        {
            IQueryable<AppointmentFailedRecordEntity> query = _db.AppointmentFailedRecords.AsNoTracking()
                .Where(f => f.CreateTime >= startDate && f.CreateTime <= endDate);
            if (execOrgIds != null && execOrgIds.Count > 0)
            {
                var execIds = execOrgIds.Distinct().AsQueryable();
                query = from f in query
                        join id in execIds on f.OrgId equals id
                        select f;
            }
            return await query.ToListAsync();
        }
    }
}