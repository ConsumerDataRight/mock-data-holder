namespace CDR.DataHolder.Banking.Domain.Entities
{
    public class Customer : Shared.Domain.Entities.Customer
    {
        public Account[]? Accounts { get; set; }
    }
}
