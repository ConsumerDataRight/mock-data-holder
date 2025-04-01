namespace CDR.DataHolder.Energy.Domain.Entities
{
    public class Customer : Shared.Domain.Entities.Customer
    {
        public EnergyAccount[]? Accounts { get; set; }
    }
}
