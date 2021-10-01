using Newtonsoft.Json;
using System.Collections.Generic;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class PushedAuthorizationBadRequestErrorResponse
    {
        [JsonProperty("errors")]
        public List<Error> Errors { get; set; }
    }

    public class Error
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }

    public class Meta
    {
    }
}