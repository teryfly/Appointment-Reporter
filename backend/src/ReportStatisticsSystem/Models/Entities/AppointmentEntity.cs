using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("appointment", Schema = "resourcepool")]
    public class AppointmentEntity
    {
        [Key]
        [Column("Id")]
        public string Id { get; set; } = string.Empty;

        [Column("CreateTime")]
        public DateTime CreateTime { get; set; }

        [Column("PatientId")]
        public string PatientId { get; set; } = string.Empty;

        [Column("Requested_Start")]
        public DateTime? RequestedStart { get; set; }

        [Column("Requested_End")]
        public DateTime? RequestedEnd { get; set; }

        [Column("Slot_Start")]
        public DateTime? SlotStart { get; set; }

        [Column("Slot_End")]
        public DateTime? SlotEnd { get; set; }

        [Column("AppointmentType")]
        public int AppointmentType { get; set; }

        [Column("OrgId")]
        public string OrgId { get; set; } = string.Empty;

        [Column("Resource_ResourceType")]
        public string? ResourceResourceType { get; set; }

        [Column("Resource_ResourceId")]
        public string? ResourceResourceId { get; set; }

        [Column("Resource_ResourceName")]
        public string? ResourceResourceName { get; set; }

        [Column("AcceptAllocate")]
        public bool AcceptAllocate { get; set; }

        [Column("Status")]
        public string Status { get; set; } = string.Empty;

        [Column("Remark")]
        public string? Remark { get; set; }

        [Column("ApplyOrgId")]
        public string? ApplyOrgId { get; set; }

        [Column("Scene")]
        public string? Scene { get; set; }

        [Column("LastUpdateTime")]
        public DateTime LastUpdateTime { get; set; }

        [Column("Resource_IsAppointment")]
        public bool? ResourceIsAppointment { get; set; }

        [Column("Ability")]
        public string? Ability { get; set; }

        [Column("LockoutTime")]
        public DateTime? LockoutTime { get; set; }

        [Column("Operator")]
        public string? Operator { get; set; }

        [Column("AlternateDeadline")]
        public DateTime? AlternateDeadline { get; set; }

        [Column("NeedConfirm")]
        public bool NeedConfirm { get; set; }

        [Column("Priority")]
        public int Priority { get; set; }

        [Column("CancelInfo_Channel")]
        public string? CancelInfoChannel { get; set; }

        [Column("CancelInfo_Operator")]
        public string? CancelInfoOperator { get; set; }

        [Column("CancelInfo_Reason")]
        public string? CancelInfoReason { get; set; }

        [Column("IsPaid")]
        public bool IsPaid { get; set; }

        [Column("CancelInfo_Mode")]
        public string? CancelInfoMode { get; set; }

        [Column("Mode")]
        public string Mode { get; set; } = string.Empty;

        [Column("Channel")]
        public string? Channel { get; set; }

        [Column("Auditor")]
        public string? Auditor { get; set; }

        [Column("AlternateSnapshot")]
        public string? AlternateSnapshot { get; set; }

        [Column("AlternateFailedCount")]
        public int AlternateFailedCount { get; set; }

        [Column("Display")]
        public string? Display { get; set; }

        [Column("PreviousId")]
        public string? PreviousId { get; set; }

        [Column("FinishMode")]
        public string FinishMode { get; set; } = string.Empty;

        [Column("Warning")]
        public string Warning { get; set; } = string.Empty;

        [Column("HospitalCode")]
        public string? HospitalCode { get; set; }

        [Column("AreaId")]
        public string? AreaId { get; set; }

        [Column("Extension")]
        public string? Extension { get; set; }

        [Column("SignInTime")]
        public DateTime? SignInTime { get; set; }

        [Column("LocationId")]
        public string? LocationId { get; set; }
    }
}