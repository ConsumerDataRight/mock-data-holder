using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class ServicePoint
    {
        [Key, MaxLength(100)]
        public string ServicePointId { get; set; } = string.Empty;

        [Required]
        public string AccountPlanId { get; set; } = string.Empty;
        public virtual AccountPlan AccountPlan { get; set; } = new AccountPlan();

        [Required, MaxLength(100)]
        public string NationalMeteringId { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string ServicePointClassification { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string ServicePointStatus { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string JurisdictionCode { get; set; } = string.Empty;
        public bool? IsGenerator { get; set; }
        public DateTime ValidFromDate { get; set; }
        public DateTime LastUpdateDateTime { get; set; }
    }
}