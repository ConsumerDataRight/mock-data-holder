using Newtonsoft.Json;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class AuthorizeClaims
    {
        [JsonProperty(PropertyName = "cdr_arrangement_id")]
        public string CdrArrangementId { get; set; }

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

		[JsonProperty(PropertyName = "value")]
		public string Value { get; set; }
	}
}