using System;

namespace CDR.DataHolder.Banking.Domain.Entities
{
    public class AccountTransaction
    {
        public string TransactionId { get; set; } = string.Empty;

        public string TransactionType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? PostingDateTime { get; set; }

        public DateTime? ValueDateTime { get; set; }

        public DateTime? ExecutionDateTime { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public string MerchantName { get; set; } = string.Empty;

        public string MerchantCategoryCode { get; set; } = string.Empty;

        public string BillerCode { get; set; } = string.Empty;

        public string BillerName { get; set; } = string.Empty;

        public string Crn { get; set; } = string.Empty;

        public string ApcaNumber { get; set; } = string.Empty;

        public string AccountId { get; set; } = string.Empty;
    }
}
