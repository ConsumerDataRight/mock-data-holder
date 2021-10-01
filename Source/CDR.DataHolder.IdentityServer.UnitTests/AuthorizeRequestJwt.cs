using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CDR.DataHolder.IdentityServer.UnitTests
{
	public class AuthorizeRequestJwt
    {
        [Required(AllowEmptyStrings = false)]
        public string Iss { get; set; }

        [Required]
        public long Iat { get; set; }

        [Required]
        public long? Exp { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Jti { get; set; }

        public string Aud { get; set; }

        [JsonProperty(PropertyName = "response_type")]
        public string ResponseType { get; set; }

        [JsonProperty(PropertyName = "client_id")]
        public string ClientId { get; set; }

        [JsonProperty(PropertyName = "redirect_uri")]
        public string RedirectUri { get; set; }

        public string Scope { get; set; }

        public string State { get; set; }

        public string Nonce { get; set; }

        public string Prompt { get; set; }

        public JwtClaims Claims { get; set; }

        public class JwtClaims
        {
            [JsonProperty(PropertyName = "cdr_arrangement_id")]
            public string CdrArrangmentId { get; set; }

            [JsonProperty(PropertyName = "sharing_duration")]
            public int? SharingDuration { get; set; }

            [JsonProperty(PropertyName = "id_token", Required = Required.Always)]
            public IdToken IdToken { get; set; }
        }

        public class IdToken
        {
            [JsonProperty(PropertyName = "acr", Required = Required.Always)]
            public Acr Acr { get; set; }
        }

        public class Acr
        {
            [JsonProperty(PropertyName = "essential")]
            public bool Essential { get; set; }

            [JsonProperty(PropertyName = "values")]
            public string[] Values { get; set; }
        }
    }
}
