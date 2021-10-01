using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class IntrospectionError
    {
        public IntrospectionError(string error, string description = null)
        {
            Error = error;
            Description = description;
        }

        [JsonPropertyName("error")]
        public string Error { get; }

        [JsonProperty("error_description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; }
    }
    public class IntrospectionSubError
    {
        public IntrospectionSubError(string error)
        {
            Error = error;
        }

        [JsonPropertyName("error")]
        public string Error { get; }
    }
}
