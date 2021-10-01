using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IClientRevocationEndpointRequestService
    {
        public Task<bool> ValidParametersReturnsOkHttpResponse(Uri revocationUri, string refreshToken);

        public Task<bool> MissingBearerTokenReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken);

        public Task<bool> InvalidTokenValueReturnsOkHttpResponse(Uri revocationUri);

        public Task<bool> InvalidSubReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken);

        public Task<bool> InvalidIssuerReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken);

        public Task<bool> InvalidAudienceReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken);

        public Task<bool> NegativeExpiryReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken);

        public Task<bool> InvalidSignatureReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken);

        public Task<bool> InvalidTokenTypeHintReturnsOkHttpResponse(Uri revocationUri, string refreshToken);

        public Task<string> GetSignedBearerTokenJwtForRevocationRequest(JwtSecurityToken jwtSecurityToken);
    }
}
