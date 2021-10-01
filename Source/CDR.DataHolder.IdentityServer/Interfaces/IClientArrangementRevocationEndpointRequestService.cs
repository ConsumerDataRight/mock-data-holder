using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IClientArrangementRevocationEndpointRequestService
    {
        public Task<bool> ValidParametersReturnsNoContentResponse(Uri arrangementRevocationUri, string cdrArrangementId);

        public Task<bool> MissingBearerTokenDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId);

        public Task<bool> InvalidCdrArrangementValueReturnsUnprocessableEntityResponse(Uri arrangementRevocationUri);

        public Task<bool> InvalidSubDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId);

        public Task<bool> InvalidIssuerDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId);

        public Task<bool> InvalidAudienceDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId);

        public Task<bool> NegativeExpiryDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId);

        public Task<bool> InvalidSignatureDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId);

        public Task<string> GetSignedBearerTokenJwtForArrangementRevocationRequest(JwtSecurityToken jwtSecurityToken);
    }
}
