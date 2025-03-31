using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class Plan
    {
        [Key]
        [MaxLength(100)]
        public string PlanId { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? DisplayName { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FuelType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string BrandName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ApplicationUri { get; set; }

        [MaxLength(100)]
        public string? CustomerType { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
