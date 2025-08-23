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
    }

    public class MedicalTechReportResponse : ReportResponseBase
    {
        public new List<MedicalTechReportItem> Data { get; set; } = new();
    }

    public class MedicalTechSourceReportItem
    {
        public string SourceType { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
    }

    public class MedicalTechSourceReportResponse : ReportResponseBase
    {
        public new List<MedicalTechSourceReportItem> Data { get; set; } = new();
    }

    public class MedicalTechItemDetailItem
    {
        public string DateKey { get; set; } = string.Empty; // 新增：按 groupBy 的时间分组键（yyyy-MM / yyyy-MM-dd / yyyy）
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class MedicalTechItemDetailResponse : ReportResponseBase
    {
        public new List<MedicalTechItemDetailItem> Data { get; set; } = new();
    }
}