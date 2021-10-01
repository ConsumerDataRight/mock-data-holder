using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class ClientArrangementRevocationRequest : ClientRequest
    {
        [JsonPropertyName("cdr_arrangement_id")]
        public string CdrArrangementId { get; set; }

        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; }
    }
}