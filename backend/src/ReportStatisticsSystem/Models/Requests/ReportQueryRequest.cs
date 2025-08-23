using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models.Requests
{
    public class ReportQueryRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // 原有执行机构/科室过滤（对应预约表 OrgId）
        public List<string>? OrgIds { get; set; }

        // 新增：申请科室（可多值，对应预约表 ApplyOrgId）
        public List<string>? ApplyOrgIds { get; set; }

        // 新增：执行科室（可多值，对应预约表 OrgId；与 OrgIds 含义一致，提供更直观的参数名）
        public List<string>? ExecOrgIds { get; set; }

        [Required]
        [RegularExpression("day|month|year", ErrorMessage = "groupBy只能为day/month/year")]
        public string GroupBy { get; set; } = "day";
    }
}