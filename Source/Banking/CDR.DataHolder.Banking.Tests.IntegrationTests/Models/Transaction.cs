namespace CDR.DataHolder.Banking.Tests.IntegrationTests.Models
{
    public class Transaction
    {
        public string TransactionId { get; set; } = null!;

        public string? TransactionType { get; set; }

        public string? Status { get; set; }

        public string Description { get; set; } = null!;

        public DateTime? PostingDateTime { get; set; }

        public DateTime? ValueDateTime { get; set; }

        public DateTime? ExecutionDateTime { get; set; }

        public double Amount { get; set; }

        public string? Currency { get; set; }

        public string Reference { get; set; } = null!;

        public string? MerchantName { get; set; }

        public string? MerchantCategoryCode { get; set; }

        public string? BillerCode { get; set; }

        public string? BillerName { get; set; }

        public string? CRN { get; set; }

        public string? ApcaNumber { get; set; }
    }
}
