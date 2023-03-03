﻿using CDR.DataHolder.Admin.API.Models;
using CDR.DataHolder.API.Infrastructure;
using CDR.DataHolder.API.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static CDR.DataHolder.API.Infrastructure.Constants;

namespace CDR.DataHolder.Admin.API.Controllers
{
    [ApiController]
    [Route("cds-au")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;

        public AdminController(
            ILogger<AdminController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("v1/admin/metrics")]
        [ApiVersion("2")]
        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetMetricsV2()
        {
            var authorizationResult = await Authorize();
            if (!authorizationResult.IsAuthorized)
            {
                return authorizationResult.SendError(Response);
            }

            // Read in the v2 data from the json file.
            var jsonFileContents = await GetFileContents(_configuration.GetValue<string>("Data:MetricsV2FileLocation"));
            Response.Headers.Add(Constants.CustomHeaders.ApiVersionHeaderKey, "2");
            return Content(ReplacePlaceholders(jsonFileContents), "application/json");
        }

        [HttpGet("v1/admin/metrics")]
        [ApiVersion("3")]
        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetMetricsV3()
        {
            var authorizationResult = await Authorize();
            if (!authorizationResult.IsAuthorized)
            {
                return authorizationResult.SendError(Response);
            }

            // Read in the v3 data from the json file.
            var jsonFileContents = await GetFileContents(_configuration.GetValue<string>("Data:MetricsV3FileLocation"));
            Response.Headers.Add(Constants.CustomHeaders.ApiVersionHeaderKey, "3");
            return Content(ReplacePlaceholders(jsonFileContents), "application/json");
        }

        private async Task<string> GetFileContents(string fileLocation)
        {
            _logger.LogDebug("Retrieving get metrics data from {fileLocation}", fileLocation);

            // Download the file contents from remote location.
            if (fileLocation.StartsWith("https://"))
            {
                using (var http = new HttpClient())
                {
                    return await http.GetStringAsync(fileLocation);
                }
            }

            // Read the file contents from local disk.
            var fileContents = System.IO.File.ReadAllText(fileLocation);
            return fileContents;
        }

        /// <summary>
        /// The Get Metrics endpoint can accept either:
        /// - an Access Token issued by the DH's auth server
        /// - a self signed JWT issued by the Register.
        /// 
        /// This method supports switching between the two auth methods using
        /// configuration, for testing purposes.
        /// </summary>
        /// <returns></returns>
        private async Task<AuthorizationResult> Authorize()
        {
            // Get the Authorization header value.
            if (this.Request.Headers.Authorization.Count == 0)
            {
                _logger.LogError("GetMetrics.Authorize: Authorization header is missing");
                return AuthorizationResult.Fail("invalid_client", "Authorization header is missing");
            }

            var authHeaderValue = this.Request.Headers.Authorization.ToString();
            if (!authHeaderValue.StartsWith("Bearer "))
            {
                _logger.LogError("GetMetrics.Authorize: Bearer token not provided");
                return AuthorizationResult.Fail("invalid_client", "Bearer token not provided");
            }

            // Bearer token needs to be a JWT.
            var token = authHeaderValue.Replace("Bearer ", "");
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            if (!jwtTokenHandler.CanReadToken(token))
            {
                _logger.LogError("GetMetrics.Authorize: Invalid token format");
                return AuthorizationResult.Fail("invalid_client", "Invalid token format");
            }

            var jwt = jwtTokenHandler.ReadJwtToken(token);

            // Self Signed JWT Authentication.
            if (jwt.Issuer == "cdr-register")
            {
                _logger.LogDebug("GetMetrics.Authorize: Self Signed JWT - {jwt}", jwt.RawData);
                return await SelfSignedJwtAuthorization(jwt);
            }

            _logger.LogDebug("GetMetrics.Authorize: Access Token JWT - {jwt}", jwt.RawData);
            return await AccessTokenAuthorization(jwt);
        }

        private async Task<AuthorizationResult> AccessTokenAuthorization(JwtSecurityToken jwt)
        {
            // Check for the required scope (admin:metrics.basic:read).
            var scopeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "scope");
            if (scopeClaim == null || !scopeClaim.Value.Contains(CdrScopes.MetricsBasicRead))
            {
                _logger.LogError("GetMetrics.Authorize: Invalid scope");
                return AuthorizationResult.Fail("invalid_scope", $"Access token is missing {CdrScopes.MetricsBasicRead} scope");
            }

            // Validate the access token.
            var dataHolderSigningKeys = await GetSigningKeys(_configuration.GetValue<string>("DataHolderJwksUri"));
            var dataHolderIssuer = _configuration.GetValue<string>("DataHolderIssuer");
            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidIssuer = dataHolderIssuer,
                ValidateAudience = true,
                ValidAudiences = new string[] { _configuration.GetValue<string>("AdminBaseUri"), "cds-au" },
                RequireAudience = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                IssuerSigningKeys = dataHolderSigningKeys,
            };

            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var result = await jwtTokenHandler.ValidateTokenAsync(jwt.RawData, tokenValidationParameters);
            if (!result.IsValid)
            {
                _logger.LogError(result.Exception, "GetMetrics.Authorize Failed");
                return AuthorizationResult.Fail("invalid_client", result.Exception.Message);
            }

            return AuthorizationResult.Pass();
        }

        private async Task<AuthorizationResult> SelfSignedJwtAuthorization(JwtSecurityToken jwt)
        {
            // Check the issuer.
            if (jwt.Issuer != "cdr-register")
            {
                _logger.LogError("GetMetrics.SelfSignedJwtAuthorization: invalid issuer");
                return AuthorizationResult.Fail("invalid_client", "Self Signed JWT Client Authentication Failed - invalid issuer");
            }

            // Check the sub.
            if (jwt.Issuer != jwt.Subject)
            {
                _logger.LogError("GetMetrics.SelfSignedJwtAuthorization: invalid sub");
                return AuthorizationResult.Fail("invalid_client", "Self Signed JWT Client Authentication Failed - invalid sub");
            }

            // Check the jti.
            var jtiClaim = jwt.Claims.FirstOrDefault(c => c.Type == "jti");
            if (jtiClaim == null || string.IsNullOrEmpty(jtiClaim.Value))
            {
                _logger.LogError("GetMetrics.SelfSignedJwtAuthorization: invalid jti");
                return AuthorizationResult.Fail("invalid_client", "Self Signed JWT Client Authentication Failed - invalid jti");
            }

            // Check the signature.
            var registerSigningKeys = await GetSigningKeys(_configuration.GetValue<string>("RegisterJwksUri"));
            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidIssuer = "cdr-register",
                ValidateAudience = true,
                ValidAudience = _configuration.GetValue<string>("AdminBaseUri"),
                RequireAudience = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                IssuerSigningKeys = registerSigningKeys,
            };

            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var result = await jwtTokenHandler.ValidateTokenAsync(jwt.RawData, tokenValidationParameters);
            if (!result.IsValid)
            {
                _logger.LogError(result.Exception, "GetMetrics.SelfSignedJwtAuthorization Failed");
                return AuthorizationResult.Fail("invalid_client", result.Exception.Message);
            }

            return AuthorizationResult.Pass();
        }

        private async Task<IEnumerable<SecurityKey>> GetSigningKeys(string jwksUri)
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                _logger.LogDebug("Retrieving JWKS from {jwksUri}", jwksUri);
                var jwksClient = new HttpClient(clientHandler);
                var jwksResponse = await jwksClient.GetAsync(jwksUri);
                var jwks = await jwksResponse.Content.ReadAsStringAsync();

                _logger.LogDebug("JWKS: {jwks}", jwks);
                return new JsonWebKeySet(jwks).Keys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred retrieving signing keys from jwks_uri");
                throw;
            }
        }

        private string ReplacePlaceholders(string json)
        {
            return json
                .Replace("#{requestTime}", DateTime.UtcNow.ToString("o"))
                .Replace("#{self}", $"{_configuration.GetValue<string>("AdminBaseUri")}/cds-au/v1/admin/metrics");
        }
    }
}
