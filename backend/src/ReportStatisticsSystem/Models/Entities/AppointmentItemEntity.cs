using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("appointment_item", Schema = "resourcepool")]
    public class AppointmentItemEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("AppointmentId")]
        public string AppointmentId { get; set; } = string.Empty;

        [Column("System")]
        public string? System { get; set; }

        [Column("Code")]
        public string? Code { get; set; }

        [Column("Name")]
        public string? Name { get; set; }

        [Column("ApplicationFormId")]
        public string? ApplicationFormId { get; set; }

        [Column("Category")]
        public string? Category { get; set; }

        [Column("ApplyNo")]
        public string? ApplyNo { get; set; }

        [Column("BodySite")]
        public string? BodySite { get; set; }

        [Column("BodySiteCode")]
        public string? BodySiteCode { get; set; }
    }

    [Table("appointment_number", Schema = "resourcepool")]
    public class AppointmentNumberEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("AppointmentId")]
        public string AppointmentId { get; set; } = string.Empty;

        [Column("NumberId")]
        public string NumberId { get; set; } = string.Empty;

        [Column("NumberNo")]
        public string? NumberNo { get; set; }
    }

    [Table("appointment_property", Schema = "resourcepool")]
    public class AppointmentPropertyEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("AppointmentId")]
        public string AppointmentId { get; set; } = string.Empty;

        [Column("PropertyType")]
        public string PropertyType { get; set; } = string.Empty;

        [Column("PropertyValue")]
        public string PropertyValue { get; set; } = string.Empty;
    }

    [Table("appointmentfailedrecord", Schema = "resourcepool")]
    public class AppointmentFailedRecordEntity
    {
        [Key]
        [Column("Id")]
        public string Id { get; set; } = string.Empty;

        [Column("Scene")]
        public string Scene { get; set; } = string.Empty;

        [Column("OrgId")]
        public string OrgId { get; set; } = string.Empty;

        [Column("PatientId")]
        public string PatientId { get; set; } = string.Empty;

        [Column("Start")]
        public System.DateTime Start { get; set; }

        [Column("End")]
        public System.DateTime End { get; set; }

        [Column("Channel")]
        public string? Channel { get; set; }

        [Column("CorrelationId")]
        public string? CorrelationId { get; set; }

        [Column("CreateTime")]
        public System.DateTime CreateTime { get; set; }

        [Column("Processed")]
        public bool Processed { get; set; }

        [Column("Processor")]
        public string? Processor { get; set; }

        [Column("ProcessorTime")]
        public System.DateTime ProcessorTime { get; set; }

        [Column("Reason")]
        public string? Reason { get; set; }

        [Column("FailureCode")]
        public string? FailureCode { get; set; }
    }
}