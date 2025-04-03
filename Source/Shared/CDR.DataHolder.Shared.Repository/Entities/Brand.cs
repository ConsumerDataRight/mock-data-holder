using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Repository.Entities
{
    public class Brand
    {
        [Key]
        [JsonProperty("dataRecipientBrandId")]
        public Guid BrandId { get; set; } = Guid.NewGuid();

        [MaxLength(100)]
        [Required]
        public string BrandName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? LogoUri { get; set; }

        [MaxLength(25)]
        [Required]
        public string Status { get; set; } = string.Empty;

        public Guid LegalEntityId { get; set; }
    }
}
