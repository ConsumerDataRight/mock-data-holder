namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class AccountPlan
    {
        public string AccountPlanId { get; set; } = null!;

        public string PlanId { get; set; } = null!;

        public string? Nickname { get; set; }

        public List<ServicePoint> ServicePoints { get; set; } = new List<ServicePoint>();

        public PlanOverview PlanOverview { get; set; } = null!;
    }
}
