namespace CDR.DataHolder.Banking.Tests.IntegrationTests.Models
{
    public class Account
    {
        public string AccountId { get; set; } = null!;

        public DateTime? CreationDate { get; set; }

        public string DisplayName { get; set; } = null!;

        public string NickName { get; set; } = null!;

        public string? OpenStatus { get; set; }

        public string MaskedName { get; set; } = null!;

        public string? ProductCategory { get; set; }

        public string ProductName { get; set; } = null!;

        public string AccountOwnership { get; set; } = null!;

        public List<Transaction>? Transactions { get; set; }
    }
}
