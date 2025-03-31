using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Repository.Entities
{
    public class Person
    {
        [Key]
        public Guid PersonId { get; set; } = Guid.NewGuid();

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleNames { get; set; }

        [MaxLength(50)]
        public string? Prefix { get; set; }

        [MaxLength(50)]
        public string? Suffix { get; set; }

        [MaxLength(20)]
        public string? OccupationCode { get; set; }

        public string? OccupationCodeVersion { get; set; }

        public DateTime? LastUpdateTime { get; set; }
    }
}
