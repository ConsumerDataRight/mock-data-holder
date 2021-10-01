using Newtonsoft.Json;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Models.BankingAccountsResponse
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
        [JsonProperty("accounts")]
        public Account[]? Accounts { get; set; }
    }

    public class Account
    {
        [JsonProperty("accountId")]
        public string? AccountId { get; set; }

        [JsonProperty("creationDate")]
        public string? CreationDate { get; set; }

        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty("nickname")]
        public string? Nickname { get; set; }

        [JsonProperty("openStatus")]
        public string? OpenStatus { get; set; }

        [JsonProperty("isOwned")]
        public bool? IsOwned { get; set; }

        [JsonProperty("maskedNumber")]
        public string? MaskedNumber { get; set; }

        [JsonProperty("productCategory")]
        public string? ProductCategory { get; set; }

        [JsonProperty("productName")]
        public string? ProductName { get; set; }
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
