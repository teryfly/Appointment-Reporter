using System.Collections.Generic;

namespace Models.Responses
{
    // data 数组中的元素：日期、科室、检查项目、门诊/住院/体检预约量与汇总
    public class MedicalTechItemRow
    {
        public string Date { get; set; } = string.Empty;     // yyyy / yyyy-MM / yyyy-MM-dd
        public string OrgId { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;

        public int OutpatientCount { get; set; }     // 门诊=01
        public int InpatientCount { get; set; }      // 住院=02
        public int PhysicalExamCount { get; set; }   // 体检=03
        public int TotalCount { get; set; }          // 合计
    }

    // 保留 success/data/total/message，并提供可选 summary
    public class MedicalTechItemSummary
    {
        public int OutpatientTotal { get; set; }
        public int InpatientTotal { get; set; }
        public int PhysicalExamTotal { get; set; }
        public int GrandTotal { get; set; }
    }

    public class MedicalTechItemResponse : ReportResponseBase
    {
        public new List<MedicalTechItemRow> Data { get; set; } = new();
        public MedicalTechItemSummary? Summary { get; set; }
    }
}