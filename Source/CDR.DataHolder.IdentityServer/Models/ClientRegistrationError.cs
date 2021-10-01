using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class ClientRegistrationError
    {
        public ClientRegistrationError(string error, string description)
        {
            Error = error;
            Description = description;
        }

        [JsonPropertyName("error")]
        public string Error { get; }

        [JsonPropertyName("error_description")]
        public string Description { get; }

    }
}