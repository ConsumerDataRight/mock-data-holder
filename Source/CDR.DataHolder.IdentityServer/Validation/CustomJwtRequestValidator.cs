using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Validation
{
    /// <summary>
    /// Validates JWT requwest objects
    /// </summary>
    public class CustomJwtRequestValidator
    {
        private readonly string _audienceUri;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// The audience URI to use
        /// </summary>
        protected string AudienceUri
        {
            get
            {
                if (_audienceUri.IsPresent())
                {
                    return _audienceUri;
                }
                else
                {
                    return _httpContextAccessor.HttpContext.GetIdentityServerIssuerUri();
                }
            }
        }

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomJwtRequestValidator"/> class.
        /// Instantiates an instance of private_key_jwt secret validator
        /// </summary>
        public CustomJwtRequestValidator(IHttpContextAccessor contextAccessor, ILogger<CustomJwtRequestValidator> logger)
        {
            _httpContextAccessor = contextAccessor;
            Logger = logger;
        }

        /// <summary>
        /// Instantiates an instance of private_key_jwt secret validator (used for testing)
        /// </summary>
        internal CustomJwtRequestValidator(string audience, ILogger<CustomJwtRequestValidator> logger)
        {
            _audienceUri = audience;
            Logger = logger;
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

            var fail = new JwtRequestValidationResult { IsError = true };

            List<SecurityKey> trustedKeys;
            try
            {
                trustedKeys = await GetKeysAsync(client);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not parse client secrets");
                return fail;
            }

            if (!trustedKeys.Any())
            {
                Logger.LogError("There are no keys available to validate JWT.");
                return fail;
            }

            JwtSecurityToken jwtSecurityToken;
            try
            {
                jwtSecurityToken = await ValidateJwtAsync(jwtTokenString, trustedKeys, client);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "JWT token validation error");
                return fail;
            }

            if (jwtSecurityToken.Payload.ContainsKey(OidcConstants.AuthorizeRequest.Request) ||
                jwtSecurityToken.Payload.ContainsKey(OidcConstants.AuthorizeRequest.RequestUri))
            {
                Logger.LogError("JWT payload must not contain request or request_uri");
                return fail;
            }

            var payload = await ProcessPayloadAsync(jwtSecurityToken);

            var result = new JwtRequestValidationResult
            {
                IsError = false,
                Payload = payload
            };

            Logger.LogDebug("JWT request object validation success.");
            return result;
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
        protected virtual Task<JwtSecurityToken> ValidateJwtAsync(string jwtTokenString, IEnumerable<SecurityKey> keys, Client client)
        {
            var uri = new Uri(_httpContextAccessor.HttpContext.GetIdentityServerIssuerUri());
           
            // Get absolute path
            int idx = uri.ToString().LastIndexOf('/');
            var path = uri.ToString().Substring(idx);

            var validAudiences = new List<string>
            {
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
                ValidateAudience = false,

                RequireSignedTokens = true,
                RequireExpirationTime = true,
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(jwtTokenString, tokenValidationParameters, out var token);

            return Task.FromResult((JwtSecurityToken)token);
        }

        /// <summary>
        /// Processes the JWT contents
        /// </summary>
        /// <param name="token">The JWT token</param>
        /// <returns></returns>
        protected virtual Task<Dictionary<string, string>> ProcessPayloadAsync(JwtSecurityToken token)
        {
            // filter JWT validation values
            var payload = new Dictionary<string, string>();
            foreach (var key in token.Payload.Keys)
            {
                if (!Filters.JwtRequestClaimTypesFilter.Contains(key))
                {
                    var value = token.Payload[key];

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
                }
            }

            return Task.FromResult(payload);
        }
    }
}
