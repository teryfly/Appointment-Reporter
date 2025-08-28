using System.Collections.Generic;

namespace Models.Responses
{
    public class DoctorAnalysisItem
    {
        public string DepartmentId { get; set; } = string.Empty; // ServiceRequest.requester (Organization id)
        public string DepartmentName { get; set; } = string.Empty;
        public string DoctorId { get; set; } = string.Empty;     // first Practitioner id from supportingInfo
        public string DoctorName { get; set; } = string.Empty;

        public int OrdersCount { get; set; }        // total from query 1
        public int AppointmentCount { get; set; }   // total from query 2
        public double AppointmentRate { get; set; } // AppointmentCount / OrdersCount * 100 (two decimals)
    }

    public class DoctorAnalysisResponse : ReportResponseBase
    {
        public new List<DoctorAnalysisItem> Data { get; set; } = new();
    }
}