namespace CDR.DataHolder.Shared.Domain.Entities
{
    public class Customer
    {
        public Guid CustomerId { get; set; }

        public string LoginId { get; set; } = string.Empty;

        public string CustomerUType { get; set; } = string.Empty;
    }
}
