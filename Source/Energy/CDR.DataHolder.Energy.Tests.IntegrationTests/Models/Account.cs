namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class Account
    {
        public string AccountId { get; set; } = null!;

        public string? OpenStatus { get; set; }

        public string? AccountNumber { get; set; }

        public string DisplayName { get; set; } = null!;

        public DateTime CreationDate { get; set; }

        public List<AccountPlan> AccountPlans { get; set; } = new List<AccountPlan>();

        public List<AccountConcession> AccountConcessions { get; set; } = new List<AccountConcession>();
    }
}
