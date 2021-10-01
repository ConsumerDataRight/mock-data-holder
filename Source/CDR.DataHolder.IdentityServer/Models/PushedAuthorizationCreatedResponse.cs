using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class PushedAuthorizationCreatedResponse
    {
        [JsonPropertyName("request_uri")]
        public string RequestUri { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
