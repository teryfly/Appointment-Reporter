using System;
using System.Collections.Generic;

namespace Models.Responses
{
    public class OutpatientReportItem
    {
        public string Date { get; set; } = string.Empty;     // 日期：yyyy / yyyy-MM / yyyy-MM-dd
        public string OrgId { get; set; } = string.Empty;    // 科室ID
        public string OrgName { get; set; } = string.Empty;  // 科室名称
        public int PersonnelCount { get; set; }              // 人员数量（医生数量）
        public int SlotCount { get; set; }                   // 放号量（号源总数）
        public int AppointmentCount { get; set; }            // 预约量（非取消状态）
        public int TotalCount { get; set; }                  // 汇总数据（所有状态预约总数）

        // 新增字段
        public string PatientId { get; set; } = string.Empty; // 来自数据库 Appointment.PatientId
        public string PatientName { get; set; } = "未知";      // 第二次遍历用 FHIR 查询补充
    }

    public class OutpatientReportResponse : ReportResponseBase
    {
        public new List<OutpatientReportItem> Data { get; set; } = new();
    }
}