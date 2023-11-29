using System;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class AccountConcession
    {
        public string? AccountConcessionId { get; set; }
        public string DisplayName { get; set; } = null!;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Type { get; set; } = null!;
        public string? Amount { get; set; }
        public string? DiscountFrequency { get; set; }
        public string? Percentage { get; set; }
        public string? AppliedTo { get; set; }
    }
}
