using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class DataRecipientRevocationResult
    {
        [JsonPropertyName("cdr_arrangement_id")]
        public string CdrArrangementId { get; set; }

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; }

        [JsonPropertyName("client_arrangement_revocation_uri")]
        public string ClientArrangementRevocationUri { get; set; }

        [JsonPropertyName("success")]
        public bool IsSuccessful { get; set; }

        [JsonPropertyName("status")]
        public HttpStatusCode? Status { get; set; }

        [JsonPropertyName("errors")]
        public List<Error> Errors { get; set; }

        public DataRecipientRevocationResult()
        {
            this.Errors = new List<Error>();
        }

    }
}
