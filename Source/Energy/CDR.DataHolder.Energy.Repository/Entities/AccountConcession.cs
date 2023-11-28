using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class AccountConcession
    {
        [Key, MaxLength(100)]
        public string AccountConcessionId { get; set; } = string.Empty;

        [Required]
        public string AccountId { get; set; } = string.Empty;
        public virtual Account Account { get; set; } = new Account();

        [MaxLength(1000), Required]
        public string Type { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? AdditionalInfo { get; set; }

        [MaxLength(1000)]
        public string? AdditionalInfoUri { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(1000)]
        public string? DiscountFrequency { get; set; }

        [MaxLength(1000)]
        public string? Amount { get; set; }

        [MaxLength(1000)]
        public string? Percentage { get; set; }

        [MaxLength(1000)]
        public string? AppliedTo { get; set; }
    }
}