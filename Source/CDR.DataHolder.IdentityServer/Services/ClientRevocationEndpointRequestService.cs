using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
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
    public class ClientRevocationEndpointRequestService : IClientRevocationEndpointRequestService
    {
        private readonly IConfigurationSettings _configurationSettings;
        private readonly ISecurityService _securityService;
        private readonly IClientRevocationEndpointHttpClient _clientRevocationEndpointHttpClient;
        private readonly ILogger _logger;
        private readonly int _defaultExpiryMinutes;

        public ClientRevocationEndpointRequestService(
            ILogger<ClientRevocationEndpointRequestService> logger,
            ISecurityService securityService,
            IConfigurationSettings configurationSettings,
            IClientRevocationEndpointHttpClient clientRevocationEndpointHttpClient)
        {
            _securityService = securityService;
            _configurationSettings = configurationSettings;
            _logger = logger;
            _defaultExpiryMinutes = 2;
            _clientRevocationEndpointHttpClient = clientRevocationEndpointHttpClient;
        }

        public async Task<bool> ValidParametersReturnsOkHttpResponse(Uri revocationUri, string refreshToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(
               claims: GetClaims(),
               issuer: "TODO:",
               audience: revocationUri.ToString(),
               expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, TokenTypes.RefreshToken),
                signedBearerTokenJwt,
                revocationUri);

            if (httpResponseMessage != null && httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Revocation Endpoint for a valid request did not return the expected 200 response. {@httpResponseMessage}", httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> MissingBearerTokenReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken)
        {
            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, TokenTypes.RefreshToken),
                string.Empty,
                revocationUri);

            if (httpResponseMessage != null
                && httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized
                && ValidWWWAuthenticateHeader(httpResponseMessage.Headers))
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    @"The DR Client Revocation Endpoint for a request with a missing bearer token did not return the expected 401 response or
                       the WwwAuthenticate header was invalid. {@httpResponseMessage}",
                    httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> InvalidTokenValueReturnsOkHttpResponse(Uri revocationUri)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: revocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues("abc123", TokenTypes.RefreshToken),
                signedBearerTokenJwt,
                revocationUri);

            if (httpResponseMessage != null && httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Revocation Endpoint for a request with an invalid token value did not return the expected 200 response. {@httpResponseMessage}", httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> InvalidSubReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims("invalid"),
                           issuer: "TODO:",
                           audience: revocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, TokenTypes.RefreshToken),
                signedBearerTokenJwt,
                revocationUri);

            if (httpResponseMessage != null
                && httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized
                && ValidWWWAuthenticateHeader(httpResponseMessage.Headers))
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    @"The DR Client Revocation Endpoint for a request with an invalid sub value did not return the expected 401 response or
                       the WwwAuthenticate header was invalid. {@httpResponseMessage}",
                    httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> InvalidIssuerReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "invalid",
                           audience: revocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, TokenTypes.RefreshToken),
                signedBearerTokenJwt,
                revocationUri);

            if (httpResponseMessage != null
                && httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized
                && ValidWWWAuthenticateHeader(httpResponseMessage.Headers))
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    @"The DR Client Revocation Endpoint for a request with an invalid issuer value did not return the expected 401 response or
                       the WwwAuthenticate header was invalid. {@httpResponseMessage}",
                    httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> InvalidAudienceReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: "http://invalidaudience.com",
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, TokenTypes.RefreshToken),
                signedBearerTokenJwt,
                revocationUri);

            if (httpResponseMessage != null
                && httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized
                && ValidWWWAuthenticateHeader(httpResponseMessage.Headers))
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    @"The DR Client Revocation Endpoint for a request with an invalid audience value did not return the expected 401 response or
                       the WwwAuthenticate header was invalid. {@httpResponseMessage}",
                    httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> NegativeExpiryReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: revocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(-2));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, TokenTypes.RefreshToken),
                signedBearerTokenJwt,
                revocationUri);

            if (httpResponseMessage != null
                && httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized
                && ValidWWWAuthenticateHeader(httpResponseMessage.Headers))
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    @"The DR Client Revocation Endpoint for a request with an invalid expiry value did not return the expected 401 response or
                       the WwwAuthenticate header was invalid. {@httpResponseMessage}",
                    httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> InvalidSignatureReturnsUnauthorizedHttpResponse(Uri revocationUri, string refreshToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: revocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var bearerTokenArray = signedBearerTokenJwt.Split(".");
            var bearerTokenJwtWithInvalidSignature = bearerTokenArray[0] + bearerTokenArray[1] + "123456abcdef";

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, TokenTypes.RefreshToken),
                bearerTokenJwtWithInvalidSignature,
                revocationUri);

            if (httpResponseMessage != null
                && httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized
                && ValidWWWAuthenticateHeader(httpResponseMessage.Headers))
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    @"The DR Client Revocation Endpoint for a request with an invalid signature value did not return the expected 401 response or
                       the WwwAuthenticate header was invalid. {@httpResponseMessage}",
                    httpResponseMessage);
                return false;
            }
        }

        public async Task<bool> InvalidTokenTypeHintReturnsOkHttpResponse(Uri revocationUri, string refreshToken)
        {
            var jwtSecurityToken = new JwtSecurityToken(
                           claims: GetClaims(),
                           issuer: "TODO:",
                           audience: revocationUri.ToString(),
                           expires: DateTime.UtcNow.AddMinutes(_defaultExpiryMinutes));

            var signedBearerTokenJwt = await GetSignedBearerTokenJwtForRevocationRequest(jwtSecurityToken);

            var httpResponseMessage = await _clientRevocationEndpointHttpClient.PostToRevocationEndPoint(
                GetFormValues(refreshToken, "invalid_hint"),
                signedBearerTokenJwt,
                revocationUri);

            if (httpResponseMessage != null && httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                _logger.LogError(
                    "The DR Client Revocation Endpoint for a request with an invalid token type hint value did not return the expected 200 response. {@httpResponseMessage}", httpResponseMessage);
                return false;
            }
        }

        public async Task<string> GetSignedBearerTokenJwtForRevocationRequest(JwtSecurityToken jwtSecurityToken)
        {
            var signingKeyName = _configurationSettings.KeyStore.SigningKeyRsa;
            //var keys = await _securityService.GetActiveSecurityKeys(signingKeyName);
            var keys = await _securityService.GetActiveSecurityKeys(SecurityAlgorithms.RsaSsaPssSha256);

            jwtSecurityToken.Header["alg"] = Algorithms.Signing.PS256;
            jwtSecurityToken.Header["kid"] = keys.First().Key.KeyId;
            jwtSecurityToken.Header["typ"] = JwtToken.JwtType;

            var plaintext = $"{jwtSecurityToken.EncodedHeader}.{jwtSecurityToken.EncodedPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);
            //using var hasher = CryptoHelper.GetHashAlgorithmForSigningAlgorithm(jwtSecurityToken.SignatureAlgorithm);
            //byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(plaintext));

            //var signature = Base64UrlTextEncoder.Encode(await _securityService.Sign(signingKeyName, jwtSecurityToken.SignatureAlgorithm, hash));
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

        private Dictionary<string, string> GetFormValues(string refreshToken, string tokenTypeHint)
        {
           return new Dictionary<string, string>
                {
                    { RevocationRequest.Token, refreshToken },
                    { RevocationRequest.TokenTypeHint, tokenTypeHint },
                };
        }

        private bool ValidWWWAuthenticateHeader(HttpResponseHeaders headers)
        {
            if (headers.WwwAuthenticate.Any(x => x.Scheme.Contains(CdsConstants.AuthenticationSchemes.AuthorizationHeaderBearer, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}
