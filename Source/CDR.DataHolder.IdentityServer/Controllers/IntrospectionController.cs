using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using CDR.DataHolder.IdentityServer.Stores;
using CDR.DataHolder.IdentityServer.Validation;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Controllers
{
    [ApiController]
    public class IntrospectionController : ControllerBase
    {
        private readonly IIdSvrService _idSvrService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRevokedTokenStore _revokedTokenStore;
        private readonly DynamicClientStore _clientStore;
        private readonly IIntrospectionRequestValidator _validator;
        private readonly ILogger _logger;
        private readonly IIdPermanenceManager _idPermanenceManager;
        private readonly IPersistedGrantStore _persistedGrantStore;

        public IntrospectionController(
            IIdSvrService idSvrService,
            IRefreshTokenService refreshTokenService,
            IRevokedTokenStore revokedTokenStore,
            DynamicClientStore clientStore,
            IIntrospectionRequestValidator validator,
            ILogger<IntrospectionController> logger,
            IPersistedGrantStore persistedGrantStore,
            IIdPermanenceManager idPermanenceManager)
        {
            _idSvrService = idSvrService;
            _refreshTokenService = refreshTokenService;
            _revokedTokenStore = revokedTokenStore;
            _persistedGrantStore = persistedGrantStore;
            _clientStore = clientStore;
            _validator = validator;
            _logger = logger;
            _idPermanenceManager = idPermanenceManager;
        }

        [HttpPost]
        [Route("connect/introspect")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Post([Required, FromForm] IntrospectionRequest request)
        {
            //Get client
            Client client = null;

            try
            {
                client = await _clientStore.FindClientByIdAsync(request.ClientId);

                if (client == null)
                {
                    return Unauthorized(new IntrospectionSubError(IntrospectionErrorCodes.InvalidClient));
                }
            }
            catch (Exception ex)
            {
                using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
                {
                    _logger.LogError(ex, "Error fetching client by client_id.");
                }
                return Unauthorized(new IntrospectionSubError(IntrospectionErrorCodes.InvalidClient));
            }

            // Validate the introspection request.
            var (isValid, invalidRequestResponse) = await ValidateRequest(request, client);
            if (!isValid)
            {
                return invalidRequestResponse;
            }

            // Check that refresh token is present
            if (string.IsNullOrEmpty(request.Token))
            {
                return Ok(new IntrospectionResult
                {
                    Active = false,
                });
            }

            // Check if the token has been revoked.
            if (await _revokedTokenStore.IsRevoked(request.Token))
            {
                return Ok(new IntrospectionResult
                {
                    Active = false,
                });
            }

            // Validate refresh token
            var result = await _refreshTokenService.ValidateRefreshTokenAsync(request.Token, client);

            // Create result
            IntrospectionResult introspectResult = null;

            if (!result.IsError && result.RefreshToken != null)
            {
                // Decrypt the subject id.
                var param = new SubPermanenceParameters()
                {
                    SoftwareProductId = result.RefreshToken.Subject.Claims.GetClaimValue("software_id"),
                    SectorIdentifierUri = result.RefreshToken.Subject.Claims.GetClaimValue("sector_identifier_uri")
                };
                var sub = _idPermanenceManager.DecryptSub(result.RefreshToken.SubjectId, param);

                // Get grant
                var grant = await _idSvrService.GetCdrArrangementGrantAsync(request.ClientId, sub);
                var scopes = string.Join(' ', result.RefreshToken.Scopes);

                introspectResult = new IntrospectionSuccessResult
                {
                    Active = true,
                    CdrArrangementId = grant.Key,
                    Expiry = result.RefreshToken.GetExpiry(),
                    Scope = scopes
                };
            }
            else
            {
                introspectResult = new IntrospectionResult
                {
                    Active = false,
                };
            }
            return Ok(introspectResult);
        }

        /// <summary>
        /// This controller action is used to check the validity of an access_token only.
        /// It should not be called by an external participant (i.e. ADR) but is consumed internally
        /// by the resource API of the mock data holder.
        /// In the CDS, the introspection endpoint only supports the introspection of refresh tokens.
        /// </summary>
        /// <param name="token">Access token to check</param>
        /// <returns>IntrospectionResult</returns>
        /// <remarks>
        /// There is currently no auth on this endpoint.  
        /// This could be added in the future to only allow the calls from the Mock Data Holder Resource API.
        /// </remarks>
        [HttpPost]
        [Route("connect/introspect-internal")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> PostInternal([Required, FromForm] string token)
        {
            // Check if the token has been revoked.
            if (string.IsNullOrEmpty(token) || await _revokedTokenStore.IsRevoked(token))
            {
                return Ok(new IntrospectionResult
                {
                    Active = false,
                });
            }

            // Check if the token is tied to an active cdr arrangement.
            // Only revoke the access token if the current client owns the access token.
            var securityToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            if (securityToken == null)
            {
                return Ok(new IntrospectionResult
                {
                    Active = false,
                });
            }

            // Perform further checking.
            var clientIdFromAccessToken = securityToken.Claims.GetClaimValue(IntrospectionRequestElements.ClientId);
            var cdrArrangementId = securityToken.Claims.GetClaimValue(StandardClaims.CDRArrangementId);
            var arrangement = await _persistedGrantStore.GetAsync(cdrArrangementId);

            // If the arrangement was not found, or has expired, or does not match the client id in the access token.
            if (arrangement == null 
                || arrangement.Expiration > DateTime.UtcNow 
                || !arrangement.ClientId.Equals(clientIdFromAccessToken, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new IntrospectionResult
                {
                    Active = false,
                });
            }

            return Ok(new IntrospectionResult
            {
                Active = IsValid(token)
            });
        }

        private static bool IsValid(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(accessToken) as JwtSecurityToken;

            if (securityToken == null)
            {
                return false;
            }

            // Check lifetime.
            if (DateTime.UtcNow < securityToken.ValidFrom)
            {
                return false;
            }

            if (DateTime.UtcNow > securityToken.ValidTo)
            {
                return false;
            }

            return true;
        }

        private async Task<(bool, IActionResult)> ValidateRequest(IntrospectionRequest request, Client client)
        {
            /*
                Jwt input formatter returns null when it cannot parse the request body or cannot parse software statetment within the request body
                When the main request body can be parsed, but not software statement, it adds validation error to ModelState.
                The rules are:
                    When the whole request body cannot be parsed we return 415 (UnsupportedMediaType)
                    When the request body can be parsed but software statemant cannot be parsed we return 400 (BadRequest)
            */
            if (request == null && ModelState.IsValid)
            {
                return (false, new UnsupportedMediaTypeResult());
            }

            if (ModelState.IsValid)
            {
                var validationResults = await _validator.ValidateAsync(request, base.HttpContext, client);

                if (validationResults.Any())
                {
                    if (validationResults.Any(x => x.ErrorMessage == IntrospectionErrorCodes.InvalidClient))
                    {
                        return (false, Unauthorized(new IntrospectionSubError(IntrospectionErrorCodes.InvalidClient)));
                    }
                    else if (validationResults.Any(x => x.ErrorMessage == IntrospectionErrorCodes.UnsupportedGrantType))
                    {
                        return (false, Unauthorized(new IntrospectionSubError(IntrospectionErrorCodes.UnsupportedGrantType)));
                    }
                    return (false, BadRequestResponse(validationResults));
                }
            }

            return (true, null);
        }

        private static IActionResult BadRequestResponse(IEnumerable<ValidationResult> validationResults)
        {
            // Client assertion errors.
            var clientAssertionErrors = validationResults.Where(x => String.Join(',', x.MemberNames) == IntrospectionRequestElements.ClientAssertion);
            if (clientAssertionErrors.Any())
            {
                return new UnauthorizedObjectResult(new IntrospectionError(IntrospectionErrorCodes.InvalidClient, clientAssertionErrors.First().ErrorMessage));
            }

            // Return the first error.
            return new BadRequestObjectResult(new IntrospectionError(IntrospectionErrorCodes.InvalidRequest, validationResults.First().ErrorMessage));
        }
    }
}
