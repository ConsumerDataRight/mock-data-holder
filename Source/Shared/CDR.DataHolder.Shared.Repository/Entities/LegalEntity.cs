using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Repository.Entities
{
    public class LegalEntity
    {
        [Key]
        public Guid LegalEntityId { get; set; } = Guid.NewGuid();

        [MaxLength(100)]
        [Required]
        public string LegalEntityName { get; set; } = string.Empty;

        [MaxLength(25)]
        [Required]
        public string Status { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? LogoUri { get; set; }
    }
}
