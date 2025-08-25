using System.Collections.Generic;

namespace Models.Responses
{
    public class DoctorAnalysisItem
    {
        public string Date { get; set; } = string.Empty; // yyyy / yyyy-MM / yyyy-MM-dd
        public string DoctorId { get; set; } = string.Empty;   // appointment.Resource_ResourceId
        public string DoctorName { get; set; } = string.Empty; // appointment.Resource_ResourceName
        public int OrdersCount { get; set; }
        public int AppointmentCount { get; set; }
        public double AppointmentRate { get; set; }
    }

    public class DoctorAnalysisResponse : ReportResponseBase
    {
        public new List<DoctorAnalysisItem> Data { get; set; } = new();
    }
}