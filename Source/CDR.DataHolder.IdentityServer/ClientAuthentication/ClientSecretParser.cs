using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using static CDR.DataHolder.API.Infrastructure.Constants;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.ClientAuthentication
{
    public class ClientSecretParser : ISecretParser
    {
        public string AuthenticationMethod => OidcConstants.EndpointAuthenticationMethods.PrivateKeyJwt;

        public async Task<ParsedSecret> ParseAsync(HttpContext context)
        {
            return context.Request.Path.ToString() switch
            {
                RequestPath.Token => await ParseTokenRequest(context),
                RequestPath.ArrangementRevocation => await ParseArrangementRevocationRequest(context),
                RequestPath.Revocation => await ParseRevocationRequest(context),
                RequestPath.Introspection => await ParseTokenRequest(context),
                _ => null,
            };
        }

        private Task<ParsedSecret> ParseTokenRequest(HttpContext context)
        {
            return ParseClientRequest<ClientTokenRequest>(context, ParsedSecretTypes.TokenSecret);
        }

        private Task<ParsedSecret> ParseArrangementRevocationRequest(HttpContext context)
        {
            static void ParseExtraParameters(IFormCollection form, ClientArrangementRevocationRequest request)
            {
                request.CdrArrangementId = form[StandardClaims.CDRArrangementId].FirstOrDefault();
                request.GrantType = form[StandardClaims.GrantType].FirstOrDefault();
            }

            return ParseClientRequest<ClientArrangementRevocationRequest>(context, ParsedSecretTypes.ArrangementRevocationSecret, ParseExtraParameters);
        }

        private Task<ParsedSecret> ParseRevocationRequest(HttpContext context)
        {
            static void ParseExtraParameters(IFormCollection form, ClientRevocationRequest request)
            {
                request.Token = form[RevocationRequest.Token].FirstOrDefault();
                request.TokenTypeHint = form[RevocationRequest.TokenTypeHint].FirstOrDefault();
            }

            return ParseClientRequest<ClientRevocationRequest>(context, ParsedSecretTypes.RevocationSecret, ParseExtraParameters);
        }

        private async Task<ParsedSecret> ParseClientRequest<T>(HttpContext context, string type, Action<IFormCollection, T> parseExtra = null)
            where T : ClientRequest, new()
        {
            var request = new T
            {
                MtlsCredential = new MtlsCredential(),
                ClientDetails = new ClientDetails(),
            };

            if (context.Request.Headers.TryGetValue(CustomHeaders.ClientCertThumbprintHeaderKey, out var certThumbprint))
            {
                request.MtlsCredential.CertificateThumbprint = certThumbprint;
            }

            if (context.Request.Headers.TryGetValue(CustomHeaders.ClientCertClientNameHeaderKey, out var certCommonName))
            {
                request.MtlsCredential.CertificateCommonName = certCommonName;
            }

            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                if (form != null)
                {
                    var clientAssertionJwt = form[OidcConstants.TokenRequest.ClientAssertion].FirstOrDefault();
                    var clientId = form[OidcConstants.TokenRequest.ClientId].FirstOrDefault();
                    // If there is not client id in the params, get it from the sub claim in client assertion
                    // FAPI test 02 - fapi1-advanced-final - doesn't have the client id in the params.
                    if (string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientAssertionJwt))
                    {
                        var jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(clientAssertionJwt);
                        var subClaim = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
                        clientId = subClaim;
                    }

                    request.ClientDetails.ClientAssertionType = form[OidcConstants.TokenRequest.ClientAssertionType].FirstOrDefault();
                    request.ClientDetails.ClientAssertion = clientAssertionJwt;
                    request.ClientDetails.ClientId = clientId;
                    request.MtlsCredential.ClientId = clientId;

                    parseExtra?.Invoke(form, request);
                }
            }

            // Default IDS4 ClientSecretValidator expects Id to be ClientId, and tries to load it from DB.
            return new ParsedSecret
            {
                Id = request.ClientDetails.ClientId,
                Credential = request,
                Type = type,
            };
        }
    }
}
