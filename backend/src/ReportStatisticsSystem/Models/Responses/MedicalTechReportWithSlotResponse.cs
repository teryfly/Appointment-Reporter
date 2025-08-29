using System.Collections.Generic;

namespace Models.Responses
{
    public class MedicalTechReportWithSlotItem
    {
        public string Slot { get; set; } = string.Empty;
        public string OrgId { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
    }

    public class MedicalTechReportWithSlotResponse : ReportResponseBase
    {
        public new List<MedicalTechReportWithSlotItem> Data { get; set; } = new();
    }
}