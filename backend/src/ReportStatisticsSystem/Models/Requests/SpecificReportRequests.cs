using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models.Requests
{
    public class OutpatientReportRequest : ReportQueryRequest
    {
        // 门诊预约特定参数可扩展
    }

    public class MedicalTechReportRequest : ReportQueryRequest
    {
        public List<string>? ExamTypes { get; set; }
    }

    public class MedicalTechSourceReportRequest : ReportQueryRequest
    {
        public List<string>? SourceTypes { get; set; }
    }

    public class MedicalTechItemDetailRequest : ReportQueryRequest
    {
        public List<string>? ItemCodes { get; set; }
    }

    public class AppointmentTimeDistributionRequest : ReportQueryRequest
    {
        [RegularExpression("hour|half-hour", ErrorMessage = "timeInterval只能为hour/half-hour")]
        public string TimeInterval { get; set; } = "hour";
    }

    public class DoctorAnalysisReportRequest : ReportQueryRequest
    {
        public List<string>? DoctorIds { get; set; }
    }
}