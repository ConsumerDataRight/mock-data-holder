namespace CDR.DataHolder.Banking.Domain.Entities
{
    public class Account : Shared.Domain.Entities.Account
    {
        public string CustomerId { get; set; } = string.Empty;

        public string NickName { get; set; } = string.Empty;

        public string MaskedName { get; set; } = string.Empty;

        public string? ProductCategory { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string AccountOwnership { get; set; } = string.Empty;

        public AccountTransaction[]? Transactions { get; set; }
    }
}
