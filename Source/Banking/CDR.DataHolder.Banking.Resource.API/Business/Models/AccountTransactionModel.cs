using System;

namespace CDR.DataHolder.Banking.Resource.API.Business.Models
{
    public class AccountTransactionModel
    {
        public string AccountId { get; set; } = string.Empty;

        public string TransactionId { get; set; } = string.Empty;

        public bool IsDetailAvailable { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? PostingDateTime { get; set; }

        public DateTime? ValueDateTime { get; set; }

        public DateTime? ExecutionDateTime { get; set; }

        public string Amount { get; set; } = string.Empty;

        public string? Currency { get; set; }

        public string Reference { get; set; } = string.Empty;

        public string? MerchantName { get; set; }

        public string MerchantCategoryCode { get; set; } = string.Empty;

        public string? BillerCode { get; set; }

        public string? BillerName { get; set; }

        public string Crn { get; set; } = string.Empty;

        public string? ApcaNumber { get; set; }
    }
}
