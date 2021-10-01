using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class IntrospectionResult
    {
        [JsonPropertyName("active")]
        public bool Active { get; set; }
    }

    public class IntrospectionSuccessResult: IntrospectionResult
    {
        [JsonPropertyName("cdr_arrangement_id")]
        public string CdrArrangementId { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("exp")]
        public int? Expiry { get; set; }
    }
}
