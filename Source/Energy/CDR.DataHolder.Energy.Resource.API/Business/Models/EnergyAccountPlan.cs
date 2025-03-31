using System;

namespace CDR.DataHolder.Energy.Resource.API.Business.Models
{
    public class EnergyAccountPlan
    {
        public string? Nickname { get; set; }

        public string[] ServicePointIds { get; set; } = Array.Empty<string>();

        public EnergyPlanOverview? PlanOverview { get; set; }
    }
}
