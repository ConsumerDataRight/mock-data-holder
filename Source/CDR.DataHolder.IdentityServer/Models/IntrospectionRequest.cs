using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class IntrospectionRequest
    {
        [FromForm(Name = "client_id")]
        [Required]
        public string ClientId { get; set; }

        [FromForm(Name = "token")]
        [Required]
        public string Token { get; set; }

        [FromForm(Name = "token_type_hint")]
        [Required]
        public string TokenTypeHint { get; set; }

        [FromForm(Name = "grant_type")]
        [Required]
        public string GrantType { get; set; }

        [FromForm(Name = "client_assertion_type")]
        [Required]
        public string ClientAssertionType { get; set; }
    }
}
