using System.Collections.Generic;
using CDR.DataHolder.API.Infrastructure.Extensions;
using IdentityModel;
using IdentityServer4.Validation;

namespace CDR.DataHolder.IdentityServer.Logging
{
    public class TokenRequestValidationLog
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string GrantType { get; set; }
        public string Scopes { get; set; }
        public string AuthorizationCode { get; set; }
        public string RefreshToken { get; set; }
        public string UserName { get; set; }
        public IEnumerable<string> AuthenticationContextReferenceClasses { get; set; }
        public string Tenant { get; set; }
        public string IdP { get; set; }

        public Dictionary<string, string> Raw { get; set; }

        private static readonly string[] SensitiveValuesFilter =
        {
            OidcConstants.TokenRequest.ClientSecret,
            OidcConstants.TokenRequest.Password,
            OidcConstants.TokenRequest.ClientAssertion,
            OidcConstants.TokenRequest.RefreshToken,
            OidcConstants.TokenRequest.DeviceCode,
        };

        public TokenRequestValidationLog(ValidatedTokenRequest request)
        {
            Raw = request.Raw.ToScrubbedDictionary(SensitiveValuesFilter);

            if (request.Client != null)
            {
                ClientId = request.Client.ClientId;
                ClientName = request.Client.ClientName;
            }

            if (request.RequestedScopes != null)
            {
                Scopes = request.RequestedScopes.ToSpaceSeparatedString();
            }

            GrantType = request.GrantType;
            AuthorizationCode = request.AuthorizationCodeHandle;
            RefreshToken = request.RefreshTokenHandle;
            UserName = request.UserName;
        }

        public override string ToString()
        {
            return LogSerializer.Serialize(this);
        }
    }
}
