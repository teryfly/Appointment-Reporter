using System.Collections.Generic;

namespace Models.Responses
{
    // 明细行：日期 + 科室 + 项目 + 三大来源分类计数 + 合计
    public class MedicalTechItemStatItem
    {
        public string Date { get; set; } = string.Empty;      // yyyy / yyyy-MM / yyyy-MM-dd
        public string OrgId { get; set; } = string.Empty;     // 科室ID
        public string OrgName { get; set; } = string.Empty;   // 科室名称
        public string ItemCode { get; set; } = string.Empty;  // 检查项目编码
        public string ItemName { get; set; } = string.Empty;  // 检查项目名称

        public int OutpatientCount { get; set; }              // 门诊预约量
        public int InpatientCount { get; set; }               // 住院预约量
        public int PhysicalExamCount { get; set; }            // 体检预约量
        public int TotalCount { get; set; }                   // 汇总（上述三类之和）
    }

    // 汇总数据：按项目/科室/合计全局等维度可在前端再聚合；此处提供全局合计
    public class MedicalTechItemStatSummary
    {
        public int OutpatientTotal { get; set; }
        public int InpatientTotal { get; set; }
        public int PhysicalExamTotal { get; set; }
        public int GrandTotal { get; set; }
    }

    public class MedicalTechItemStatResponse : ReportResponseBase
    {
        public new List<MedicalTechItemStatItem> Data { get; set; } = new();
        public MedicalTechItemStatSummary Summary { get; set; } = new();
    }
}