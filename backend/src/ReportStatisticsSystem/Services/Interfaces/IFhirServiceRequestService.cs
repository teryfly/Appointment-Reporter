using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public class ServiceRequestFilter
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string ProfileUrl { get; set; } = "http://StructureDefinition.hl7fhir.cn/inspect-request";
        public List<string>? ExecOrgIds { get; set; } // requester Organization ids
        public List<string>? DoctorIds { get; set; }  // supportingInfo Practitioner ids
    }

    public class ServiceRequestStatKey
    {
        public string DepartmentId { get; set; } = string.Empty; // requester Organization id
        public string DoctorId { get; set; } = string.Empty;     // supportingInfo Practitioner id (first only)
    }

    public interface IFhirServiceRequestService
    {
        // Count authoredOn in [Start, End], profile matched, status != revoked and != entered-in-error
        Task<Dictionary<ServiceRequestStatKey, int>> CountOrdersAsync(ServiceRequestFilter filter);

        // Same as orders plus intent == filler-order
        Task<Dictionary<ServiceRequestStatKey, int>> CountAppointmentsAsync(ServiceRequestFilter filter);
    }
}