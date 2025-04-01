using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class PlanAdditionalInformation
    {
        [MaxLength(100)]
        public string? PlanId { get; set; }

        public Plan? Plan { get; set; }

        [MaxLength(1000)]
        public string? OverviewUri { get; set; }

        [MaxLength(1000)]
        public string? TermsUri { get; set; }

        [MaxLength(1000)]
        public string? EligibilityUri { get; set; }

        [MaxLength(1000)]
        public string? PricingUri { get; set; }

        [MaxLength(1000)]
        public string? BundleUri { get; set; }
    }
}
