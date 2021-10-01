using Newtonsoft.Json;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Models.BankingTransactionsResponse
{
    public class Response
    {
        [JsonProperty("data")]
        public Data? Data { get; set; }

        [JsonProperty("links")]
        public Links? Links { get; set; }

        [JsonProperty("meta")]
        public Meta? Meta { get; set; }
    }

    public class Data
    {
        [JsonProperty("transactions")]
        public Transaction[]? Transactions { get; set; }
    }

    public class Transaction
    {
        [JsonProperty("accountId")]
        public string? AccountId { get; set; }

        [JsonProperty("transactionId")]
        public string? TransactionId { get; set; }

        [JsonProperty("isDetailAvailable")]
        public bool? IsDetailAvailable { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("postingDateTime")]
        public string? PostingDateTime { get; set; }

        [JsonProperty("valueDateTime")]
        public string? ValueDateTime { get; set; }

        [JsonProperty("executionDateTime")]
        public string? ExecutionDateTime { get; set; }

        [JsonProperty("amount")]
        public string? Amount { get; set; }

        [JsonProperty("currency")]
        public string? Currency { get; set; }

        [JsonProperty("reference")]
        public string? Reference { get; set; }

        [JsonProperty("merchantName")]
        public string? MerchantName { get; set; }

        [JsonProperty("merchantCategoryCode")]
        public string? MerchantCategoryCode { get; set; }

        [JsonProperty("billerCode")]
        public string? BillerCode { get; set; }

        [JsonProperty("billerName")]
        public string? BillerName { get; set; }

        [JsonProperty("crn")]
        public string? Crn { get; set; }

        [JsonProperty("apcaNumber")]
        public string? ApcaNumber { get; set; }
    }

    public class Links
    {
        [JsonProperty("first")]
        public string? First { get; set; }

        [JsonProperty("last")]
        public string? Last { get; set; }

        [JsonProperty("next")]
        public string? Next { get; set; }

        [JsonProperty("prev")]
        public string? Prev { get; set; }

        [JsonProperty("self")]
        public string? Self { get; set; }
    }

    public class Meta
    {
        [JsonProperty("totalRecords")]
        public long? TotalRecords { get; set; }

        [JsonProperty("totalPages")]
        public long? TotalPages { get; set; }
    }
}
