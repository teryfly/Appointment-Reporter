using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entities
{
    [Table("organization")]
    public class OrganizationEntity
    {
        [Column("sequence")]
        public string? Sequence { get; set; }

        [Column("telecom")]
        public string? Telecom { get; set; }

        [Column("id")]
        public string Id { get; set; } = string.Empty;

        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}