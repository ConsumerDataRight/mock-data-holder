using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class CdrArrangementGrant
    {
        [JsonPropertyName("refresh_token_key")]
        public string RefreshTokenKey { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("auth_code")]
        public string AuthCode { get; set; }
    }
}