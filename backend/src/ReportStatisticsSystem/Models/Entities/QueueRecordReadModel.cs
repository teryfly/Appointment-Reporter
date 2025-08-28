using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    // Read-only entity mapped to [isp-scheduler].[schedule].QueueRecords
    // This model is used to aggregate outpatient visits (就诊量) per hour.
    [Table("QueueRecords", Schema = "schedule")]
    public class QueueRecordReadModel
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Column("Status")]
        public string Status { get; set; } = string.Empty; // '5' means completed/visited

        [Column("IsDeleted")]
        public int IsDeleted { get; set; }

        [Column("QueueType")]
        public string QueueType { get; set; } = string.Empty; // '01' outpatient

        // Department id/name columns in scheduler DB
        [Column("itemCode")]
        public string? ItemCode { get; set; } // 科室id

        [Column("itemName")]
        public string? ItemName { get; set; } // 科室名称

        [Column("doctorname")]
        public string? DoctorName { get; set; } // 医生名称

        [Column("appointmentSourceStartTime")]
        public DateTime AppointmentSourceStartTime { get; set; } // 就诊开始时间（用于按小时聚合）
    }
}