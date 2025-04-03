using CDR.DataHolder.Shared.Domain.Entities;

namespace CDR.DataHolder.Energy.Domain.Entities
{
    public class EnergyAccount : Account
    {
        public string? AccountNumber { get; set; }

        public EnergyAccountPlan[]? Plans { get; set; }

        public EnergyAccountConcession[]? Concessions { get; set; }
    }
}
