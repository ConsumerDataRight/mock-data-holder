using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Services;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Validation
{
    /// <summary>
    /// Validates JWT requwest objects
    /// </summary>
    public class CustomJwtRequestValidator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IClientService _clientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomJwtRequestValidator"/> class.
        /// Instantiates an instance of private_key_jwt secret validator
        /// </summary>
        public CustomJwtRequestValidator(
            ILogger<CustomJwtRequestValidator> logger,
            IConfiguration configuration,
            IClientService clientService)
        {
            _configuration = configuration;
            _logger = logger;
            _clientService = clientService;
        }

        /// <summary>
        /// Validates a JWT request object
        /// </summary>
        /// <param name="client">The client</param>
        /// <param name="jwtTokenString">The JWT</param>
        /// <returns></returns>
        public virtual async Task<JwtRequestValidationResult> ValidateAsync(Client client, string jwtTokenString)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (String.IsNullOrWhiteSpace(jwtTokenString)) throw new ArgumentNullException(nameof(jwtTokenString));

            List<SecurityKey> trustedKeys;
            try
            {
                trustedKeys = await GetKeysAsync(client);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not parse client secrets");
                return BadRequest("Keys not found to validation signature");
            }

            if (!trustedKeys.Any())
            {
                _logger.LogError("There are no keys available to validate JWT.");
                return BadRequest("Keys not found to validation signature");
            }

            JwtSecurityToken jwtSecurityToken;
            try
            {
                jwtSecurityToken = await ValidateJwtAsync(jwtTokenString, trustedKeys, client);
            }
            catch (SecurityTokenInvalidSignatureException e)
            {
                _logger.LogError(e, "JWT signature validation error");
                return BadRequest("JWT signature validation error");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "JWT validation error");
                return BadRequest("JWT validation error");
            }

            if (jwtSecurityToken.Payload.ContainsKey(OidcConstants.AuthorizeRequest.Request) ||
                jwtSecurityToken.Payload.ContainsKey(OidcConstants.AuthorizeRequest.RequestUri))
            {
                _logger.LogError("JWT payload must not contain request or request_uri");
                return BadRequest("JWT payload must not contain request or request_uri");
            }
            
            // Validate the alg
            //E.g. fapi1-advanced-final-ensure-signed-client-assertion-with-RS256-fails, fapi1-advanced-final-ensure-signed-request-object-with-RS256-fails
			var expectedAlgs = new string[] { Algorithms.Signing.PS256, Algorithms.Signing.ES256, }; // Maybe get it from the config of the Client?
			if (jwtSecurityToken.Header?.Alg == null || !expectedAlgs.Contains(jwtSecurityToken.Header?.Alg))
			{
                _logger.LogError("Invalid Alg header");
                return BadRequest("Invalid Alg header");
            }

            var payload = await ProcessPayloadAsync(jwtSecurityToken);

            var result = new JwtRequestValidationResult
            {
                IsError = false,
                Payload = payload
            };

            _logger.LogDebug("JWT request object validation success.");
            return result;
        }

        private JwtRequestValidationResult BadRequest(string description)
        {
            return new JwtRequestValidationResult()
            {
                IsError = true,
                Error = AuthorizeErrorCodes.InvalidRequestObject,
                ErrorDescription = description
            };
        }
        
        /// <summary>
        /// Retrieves keys for a given client
        /// </summary>
        /// <param name="client">The client</param>
        /// <returns></returns>
        protected virtual Task<List<SecurityKey>> GetKeysAsync(Client client)
        {
            return client.ClientSecrets.GetKeysAsync();
        }

        /// <summary>
        /// Validates the JWT token
        /// </summary>
        /// <param name="jwtTokenString">JWT as a string</param>
        /// <param name="keys">The keys</param>
        /// <param name="client">The client</param>
        /// <returns></returns>
        protected virtual async Task<JwtSecurityToken> ValidateJwtAsync(string jwtTokenString, IEnumerable<SecurityKey> keys, Client client)
        {
            var validAudiences = new List<string>
            {
                _configuration.GetValue<string>(Constants.ConfigurationKeys.IssuerUri)
            };

            // The code does not validate the Audience and Issuer fields as its presence is only SHOULD as per spec
            // https://openid.net/specs/openid-connect-core-1_0.html#HybridAuthorizationEndpoint
            // If signed, the Request Object SHOULD contain the Claims iss (issuer) and aud (audience) as members.
            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKeys = keys,
                ValidateIssuerSigningKey = true,

                ValidIssuer = client.ClientId,
                ValidateIssuer = false,

                ValidAudiences = validAudiences,
                ValidateAudience = true,

                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                LifetimeValidator = (DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters) =>
                {
                    //e.g. fapi1-advanced-final-ensure-request-object-without-exp-fails
                    //fapi1-advanced-final-ensure-request-object-without-nbf-fails                
                    //fapi1-advanced-final-ensure-request-object-with-exp-over-60-fails: https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.6.1
                    //fapi1-advanced-final-ensure-request-object-with-nbf-over-60-fails

                    //exp cannot be older than 60 mins in the past
                    if (expires.HasValue && (expires.Value.Subtract(DateTime.UtcNow) > TimeSpan.FromMinutes(60)))
                    {
                        return false;
                    }

                    // Only validate if the request has nbf and the configuration requires it.
                    if (notBefore == null) 
                    {
                        if (_configuration.FapiComplianceLevel() >= FapiComplianceLevel.Fapi1Phase2)
                        {
                            return false;
                        }

                        return true;
                    }

                    //nbf cannot be longer than 60 mins in the past 
                    if ((DateTime.UtcNow.Subtract(notBefore.Value) > TimeSpan.FromMinutes(60)))
                    {
                        return false;
                    }

                    //nbf cannot be after expiry
                    //expiry cannot be longer than 60 min after not before 
                    if (notBefore.Value > expires.Value || (expires.Value.Subtract(notBefore.Value) > TimeSpan.FromMinutes(60)))
                    {
                        return false;
                    }

                    return true;
                }
            };

            var handler = new JwtSecurityTokenHandler();
            await _clientService.EnsureKid(client.ClientId, jwtTokenString, tokenValidationParameters);
            handler.ValidateToken(jwtTokenString, tokenValidationParameters, out var token);
            return (JwtSecurityToken)token;
        }

        /// <summary>
        /// Processes the JWT contents
        /// </summary>
        /// <param name="token">The JWT token</param>
        /// <returns></returns>
        protected virtual Task<Dictionary<string, string>> ProcessPayloadAsync(JwtSecurityToken token)
        {
            _logger.LogDebug("{method}: token = {token}", nameof(ProcessPayloadAsync), token);

            // filter JWT validation values
            var payload = new Dictionary<string, string>();
            foreach (var key in token.Payload.Keys)
            {
                if (!Filters.JwtRequestClaimTypesFilter.Contains(key))
                {
                    var value = token.Payload[key];
                    _logger.LogDebug("{method}: value = {value}", nameof(ProcessPayloadAsync), value);

                    if (value is string s)
                    {
                        payload.Add(key, s);
                    }
                    else if (value is JObject jobj)
                    {
                        payload.Add(key, jobj.ToString(Formatting.None));
                    }
                    else if (value is JArray jarr)
                    {
                        payload.Add(key, jarr.ToString(Formatting.None));
                    }
                    // .NET 6 fix - JSON values are no longer stored as a NewtonSoft.Json.Linq.JObject type so the condition above fails.
                    // Therefore this new condition is added as a catchall.
                    else if (value != null)
                    {
                        payload.Add(key, value.ToString());
                    }
                }
            }

            return Task.FromResult(payload);
        }
    }
}
