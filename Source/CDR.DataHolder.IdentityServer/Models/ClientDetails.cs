using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class ClientDetails
    {
        public string ClientAssertion { get; set; }

        public string ClientAssertionType { get; set; }

        public string ClientId { get; set; }

        public SecurityKey[] TrustedKeys { get; set; }
    }
}