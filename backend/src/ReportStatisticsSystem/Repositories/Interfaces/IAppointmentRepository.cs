using Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<List<AppointmentEntity>> GetAppointmentsAsync(
            DateTime startDate,
            DateTime endDate,
            List<string>? execOrgIds,
            int? appointmentType = null,
            List<string>? applyOrgIds = null);

        Task<List<AppointmentItemEntity>> GetAppointmentItemsByIdsAsync(List<string> appointmentIds);
        Task<List<AppointmentNumberEntity>> GetAppointmentNumbersByIdsAsync(List<string> appointmentIds);
        Task<List<AppointmentPropertyEntity>> GetAppointmentPropertiesByIdsAsync(List<string> appointmentIds);
        Task<List<AppointmentFailedRecordEntity>> GetAppointmentFailedRecordsAsync(DateTime startDate, DateTime endDate, List<string>? execOrgIds);
    }
}