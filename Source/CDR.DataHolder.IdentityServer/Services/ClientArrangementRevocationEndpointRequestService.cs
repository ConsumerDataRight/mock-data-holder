using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public class ClientArrangementRevocationEndpointRequestService : IClientArrangementRevocationEndpointRequestService
    {
        private readonly IConfigurationSettings _configurationSettings;
        private readonly ISecurityService _securityService;
        private readonly IClientArrangementRevocationEndpointHttpClient _clientArrangementRevocationEndpointHttpClient;
        private readonly ILogger _logger;
        private readonly int _defaultExpiryMinutes;

        public ClientArrangementRevocationEndpointRequestService(
            ILogger<ClientArrangementRevocationEndpointRequestService> logger,
            ISecurityService securityService,
            IConfigurationSettings configurationSettings,
            IClientArrangementRevocationEndpointHttpClient clientArrangementRevocationEndpointHttpClient)
        {
            _securityService = securityService;
            _configurationSettings = configurationSettings;
            _logger = logger;
            _defaultExpiryMinutes = 2;
            _clientArrangementRevocationEndpointHttpClient = clientArrangementRevocationEndpointHttpClient;
        }

        public async Task<bool> ValidParametersReturnsNoContentResponse(Uri arrangementRevocationUri, string cdrArrangementId)
        {
            var jwtSecurityToken = new JwtSecurityToken(
               claims: GetClaims(),
               issuer: "TODO:",
               audience: arrangementRevocationUri.ToString(),
               expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues(cdrArrangementId),
                signedBearerTokenJwt,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a valid request did not return the expected No Content response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<bool> MissingBearerTokenDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId)
        {
            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues(cdrArrangementId),
                string.Empty,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode != HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a request with a missing bearer token returned a No Content Response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<bool> InvalidCdrArrangementValueReturnsUnprocessableEntityResponse(Uri arrangementRevocationUri)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                claims: GetClaims(),
                audience: arrangementRevocationUri.ToString(),
                expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues("abc123"),
                signedBearerTokenJwt,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode == HttpStatusCode.UnprocessableEntity)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a request with an invalid token value did not return the expected Unprocessable Entity response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<bool> InvalidSubDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims("invalid"),
                           issuer: "TODO:",
                           audience: arrangementRevocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues(cdrArrangementId),
                signedBearerTokenJwt,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode != HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a request with an invalid sub value returned a No Content Response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<bool> InvalidIssuerDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "invalid",
                           audience: arrangementRevocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues(cdrArrangementId),
                signedBearerTokenJwt,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode != HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a request with an invalid issuer value returned a No Content Response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<bool> InvalidAudienceDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: "http://invalidaudience.com",
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues(cdrArrangementId),
                signedBearerTokenJwt,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode != HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a request with an invalid audience value returned a No Content Response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<bool> NegativeExpiryDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: arrangementRevocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(-2));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues(cdrArrangementId),
                signedBearerTokenJwt,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode != HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a request with an invalid expiry value returned a No Content Response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<bool> InvalidSignatureDoesNotReturnNoContentHttpResponse(Uri arrangementRevocationUri, string cdrArrangementId)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: arrangementRevocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForArrangementRevocationRequest(jwtSecurityToken);

            var bearerTokenArray = signedBearerTokenJwt.Split(".");
            var bearerTokenJwtWithInvalidSignature = bearerTokenArray[0] + bearerTokenArray[1] + "123456abcdef";

            var httpResponseStatusCode = await _clientArrangementRevocationEndpointHttpClient.PostToArrangementRevocationEndPoint(
                GetFormValues(cdrArrangementId),
                bearerTokenJwtWithInvalidSignature,
                arrangementRevocationUri);

            if (httpResponseStatusCode.HasValue && httpResponseStatusCode != HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Arrangement Revocation Endpoint for a request with an invalid signature value returned a No Content Response. {StatusCode}",
                    GetHttpResponseStatusCodeMessageForLog(httpResponseStatusCode));
                return false;
            }
        }

        public async Task<string> GetSignedBearerTokenJwtForArrangementRevocationRequest(JwtSecurityToken jwtSecurityToken)
        {
            var keys = await _securityService.GetActiveSecurityKeys(SecurityAlgorithms.RsaSsaPssSha256);

            jwtSecurityToken.Header["alg"] = Algorithms.Signing.PS256;
            jwtSecurityToken.Header["kid"] = keys.First().Key.KeyId;
            jwtSecurityToken.Header["typ"] = JwtToken.JwtType;

            var plaintext = $"{jwtSecurityToken.EncodedHeader}.{jwtSecurityToken.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);
            var signature = Base64UrlTextEncoder.Encode(await _securityService.Sign(jwtSecurityToken.SignatureAlgorithm, digest));

            return $"{plaintext}.{signature}";
        }

        private Claim[] GetClaims(string subClaim = null)
        {
            subClaim ??= "TODO:";

            return new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subClaim),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
        }

        private Dictionary<string, string> GetFormValues(string cdrArrangementId)
        {
           return new Dictionary<string, string>
                {
                    { CdrArrangementRevocationRequest.CdrArrangementId, cdrArrangementId },
                };
        }

        private string GetHttpResponseStatusCodeMessageForLog(HttpStatusCode? httpStatusCode)
        {
            return httpStatusCode.HasValue ? $"Status Code: {((int)httpStatusCode.Value).ToString()}" : "HttpStatusCode is null, check error log";
        }
    }
}
