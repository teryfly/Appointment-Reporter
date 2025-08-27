using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    // 注意：以下列名根据错误日志校正：不存在 IsDelete 列
    // 若实际列名与本定义不符，请据实修改 Column 属性
    [Table("number_provider", Schema = "resourcepool")]
    public class NumberProviderEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        // 执行科室ID（与 appointment.OrgId 对齐）
        [Column("OrgId")]
        public string OrgId { get; set; } = string.Empty;

        // 医生ID（与 appointment.Resource_ResourceId 对齐）
        [Column("ResourceId")]
        public string DoctorId { get; set; } = string.Empty;

        // 放号时间（number_provider.Start：为业务统计口径）
        [Column("Start")]
        public DateTime Start { get; set; }

        // 号源数量
        [Column("NumberCount")]
        public int NumberCount { get; set; }

        // 场景：门诊=01
        [Column("Scene")]
        public string? Scene { get; set; }

        // 保留 OccurTime（映射 CreateTime）兼容旧用法
        [NotMapped]
        public DateTime OccurTime
        {
            get => Start;
            set { Start = value; }
        }
    }
}