namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class PlanOverview
    {
        public string PlanOverviewId { get; set; } = null!;

        public string? DisplayName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
