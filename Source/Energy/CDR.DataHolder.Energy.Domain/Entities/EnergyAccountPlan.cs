namespace CDR.DataHolder.Energy.Domain.Entities
{
    public class EnergyAccountPlan
    {
        public string? Nickname { get; set; }

        public EnergyServicePoint[]? ServicePoints { get; set; }

        public EnergyPlanOverview? PlanOverview { get; set; }
    }
}
