using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Xunit;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.UnitTests.AuthorizeRequestJwt;

namespace CDR.DataHolder.IdentityServer.UnitTests
{
    public class AuthoriseTests
    {
        public AuthoriseTests()
        {

        }

        [Fact]
        public async Task CreateAuthoriseToken_Success()
        {
            /**
             * Note: This requires DR certs or APIs on the DR to get these tokens. For now, cert is copied from the SSA endpoint to this
             */

            // Arrange

            string clientId = "ac345cd3-98dd-4f6d-b58c-af686cf4c551"; // This is the Clients.ClientId after the client registration.
            var requestObject = GetAuthorizeRequest(clientId);
            var requestJwtString = JwtHelper.GetJwtValidSignature(requestObject);

            // Get the client assertion for token endpoint
            var clientAssertion = GetClientAssertion(clientId);
            //var kid = "542A9B91600488088CD4D816916A9F4488DD2651"; // This is the kid of the Clients.ClientSecrets after the registration is done.
            var clientAssertionJwtString = JwtHelper.GetJwtValidSignature(clientAssertion);
            //TODO: get the signing keys for the validation.
            //JwtHelper.X509CertificateForValidTesting().PublicKey.

            // Act
            //var tokenValidationParameters = new TokenValidationParameters
            //{
            //    IssuerSigningKeys = keys,
            //    ValidateIssuerSigningKey = true,

            //    ValidIssuer = clientId,
            //    ValidateIssuer = false,

            //    ValidateAudience = false,

            //    RequireSignedTokens = true,
            //    RequireExpirationTime = true,
            //};

            //var handler = new JwtSecurityTokenHandler();
            //handler.ValidateToken(requestJwtString, tokenValidationParameters, out var token);


            // Assert
        }


        private static AuthorizeRequestJwt GetAuthorizeRequest(string clientId, string responseType = ResponseTypes.CodeIdToken, string scope=null, string cdrArrangmentId = null)
        {
            var acrValues = new string[1] { StandardClaims.ACR2Value };
            var iatDatetime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            return new AuthorizeRequestJwt()
            {
                Iss = clientId,
                Iat = iatDatetime,
                Jti = Guid.NewGuid().ToString().Replace("-", string.Empty),
                Exp = iatDatetime + 14400, //iatDatetime + 1800, TODO: undo only for dev testing.
                Aud = $"https://localhost:8001",
                ResponseType = responseType,
                ClientId = clientId,
                RedirectUri = "https://fintechx.io/products/trackxpense/cb",
                Scope = scope ?? $"{StandardScopes.OpenId} {StandardScopes.Profile} bank:accounts.detail:read",
                State = AuthorizeRequest.State,
                Nonce = AuthorizeRequest.Nonce,
                Claims = new JwtClaims()
                {
                    CdrArrangmentId = cdrArrangmentId,
                    SharingDuration = 7776000,
                    IdToken = new IdToken()
                    {
                        Acr = new Acr() { Essential = true, Values = acrValues },
                    },
                }
            };
        }

        public static ClientAssertionJwt GetClientAssertion(string clientId)
        {
            var iatDatetime = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return new ClientAssertionJwt()
            {
                Iss = clientId,
                Sub = clientId,
                Iat = iatDatetime,
                Jti = Guid.NewGuid().ToString().Replace("-", string.Empty),
                Exp = iatDatetime + 14400,// 1800,
                Aud = $"https://localhost:8001/connect/token",
            };
        }
    }
}
