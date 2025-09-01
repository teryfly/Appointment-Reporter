using System.Collections.Generic;

namespace Models.Responses
{
    public class MedicalTechReportItem
    {
        public string ExamType { get; set; } = string.Empty;
        public string OrgId { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }

        // Optional slot field (日期/时段)
        public string? Slot { get; set; }
    }

    public class MedicalTechReportResponse : ReportResponseBase
    {
        public new List<MedicalTechReportItem> Data { get; set; } = new();
    }

    public class MedicalTechSourceReportItem
    {
        // Deprecated in new spec; kept for compatibility (unused)
        public string SourceType { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
    }

    // New spec: flattened rows with orgId/orgName/slot and three source counts + total
    public class MedicalTechSourceReportResponse : ReportResponseBase
    {
        public new List<MedicalTechSourceFlatItem> Data { get; set; } = new();
    }

    public class MedicalTechSourceFlatItem
    {
        public string OrgId { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public string Slot { get; set; } = string.Empty; // yyyy / yyyy-MM / yyyy-MM-dd according to groupBy
        public int OutpatientCount { get; set; }     // source-type code=1
        public int InpatientCount { get; set; }      // source-type code=2
        public int PhysicalExamCount { get; set; }   // source-type code=3
        public int TotalCount { get; set; }          // sum of above
    }
}