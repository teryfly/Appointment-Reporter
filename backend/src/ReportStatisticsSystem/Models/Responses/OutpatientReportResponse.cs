using System;
using System.Collections.Generic;

namespace Models.Responses
{
    public class OutpatientReportItem
    {
        public string OrgId { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int PersonnelCount { get; set; }
        public int SlotCount { get; set; }
        public int AppointmentCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }

        // 新增：医生ID（用于时段分布等需要医生维度的结果；非时段分布场景可为空）
        public string? DoctorId { get; set; }
    }

    public class OutpatientReportResponse : ReportResponseBase
    {
        public new List<OutpatientReportItem> Data { get; set; } = new();
    }
}