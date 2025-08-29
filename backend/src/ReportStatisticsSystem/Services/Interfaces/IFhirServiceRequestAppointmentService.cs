using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public class ServiceRequestAppointmentFilter
    {
        // Use occurrence[x] (DateTime) range inclusive
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        // performer Organization ids filter (execution departments)
        public List<string>? ExecOrgIds { get; set; }
        // profile url (fixed default)
        public string ProfileUrl { get; set; } = "http://StructureDefinition.hl7fhir.cn/inspect-request";
    }

    public class AppointmentStatKey
    {
        public string PerformerOrgId { get; set; } = string.Empty; // ServiceRequest.performer -> Organization/{id}
        public string CategoryCode { get; set; } = string.Empty;   // category.coding where system == http://CodeSystem.hl7fhir.cn/inspect-category
    }

    public interface IFhirServiceRequestAppointmentService
    {
        Task<Dictionary<AppointmentStatKey, int>> CountAppointmentsAsync(ServiceRequestAppointmentFilter filter);
    }
}