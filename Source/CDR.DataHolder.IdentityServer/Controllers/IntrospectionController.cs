using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
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
        private readonly IReferenceTokenStore _referenceTokenStore;
        private readonly DynamicClientStore _clientStore;
        private readonly IIntrospectionRequestValidator _validator;
        private readonly ILogger _logger;
        private readonly IIdPermanenceManager _idPermanenceManager;
        private readonly IPersistedGrantStore _persistedGrantStore;

        public IntrospectionController(IIdSvrService idSvrService,
                                        IRefreshTokenService refreshTokenService,
                                        IReferenceTokenStore referenceTokenStore,
                                        DynamicClientStore clientStore,
                                        IIntrospectionRequestValidator validator,
                                        ILogger<IntrospectionController> logger,
                                        IIdPermanenceManager idPermanenceManager,
                                        IPersistedGrantStore persistedGrantStore)
        {
            _idSvrService = idSvrService;
            _refreshTokenService = refreshTokenService;
            _referenceTokenStore = referenceTokenStore;
            _clientStore = clientStore;
            _validator = validator;
            _logger = logger;
            _idPermanenceManager = idPermanenceManager;
            _persistedGrantStore = persistedGrantStore;
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
                _logger.LogError(ex, "Error fetching client by client_id.");
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

            //Validate refresh token
            var result = await _refreshTokenService.ValidateRefreshTokenAsync(request.Token, client);

            //Create result
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

                int expiry = int.Parse(result.RefreshToken.Subject.Claims.Single(x => x.Type == StandardClaims.SharingDurationExpiresAt).Value);
                var scopes = string.Join(' ', result.RefreshToken.Scopes);

                introspectResult = new IntrospectionSuccessResult
                {
                    Active = true,
                    CdrArrangementId = grant.Key,
                    Expiry = expiry,
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
            var tokenIsValid = true;
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            IEnumerable<Claim> claims = securityToken.Claims;

            // Does the TOKEN exist in the Memory Cache? - if YES then is is a VALID TOKEN
            var at = await _referenceTokenStore.GetReferenceTokenAsync(token);
            if (at == null)
            {
                // IF NO - is is still a valid TOKEN?
                Int32 nbf = Convert.ToInt32(claims.Where(p => p.Type == "nbf").FirstOrDefault()?.Value);
                Int32 exp = Convert.ToInt32(claims.Where(p => p.Type == "exp").FirstOrDefault()?.Value);
                TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                Int32 nowInSecs = Convert.ToInt32(timeSpan.TotalSeconds);
                if (nbf > nowInSecs && exp < nowInSecs)
                {
                    tokenIsValid = false;
                }
            }

            string clientId;
            if (!string.IsNullOrEmpty(token))
            {
                // IF the User Consent exists in the Persisted Grants store
                clientId = claims.Where(p => p.Type == "client_id").FirstOrDefault()?.Value;
                var keys = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { ClientId = clientId });
                var user = keys.FirstOrDefault(g => g.Type == "user_consent" && g.ClientId == clientId);
                if (user != null || !string.IsNullOrEmpty(user.Key))
                {
                    // AND the REFRESH TOKEN has been revoked, then the ACCESS TOKEN is now INVALID.
                    var refTkn = keys.FirstOrDefault(g => g.Type == TokenTypes.RefreshToken && g.ClientId == clientId);
                    if (refTkn == null || string.IsNullOrEmpty(refTkn.Key))
                    {
                        tokenIsValid = false;
                    }
                }
            }
            if (tokenIsValid)
                return Ok(new IntrospectionResult
                {
                    Active = tokenIsValid,
                });
            else
                return Unauthorized(new IntrospectionResult
                {
                    Active = tokenIsValid,
                });
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

        private IActionResult BadRequestResponse(IEnumerable<ValidationResult> validationResults)
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
