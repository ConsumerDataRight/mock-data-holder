using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Helpers;
using CDR.DataHolder.IdentityServer.Logging;
using CDR.DataHolder.IdentityServer.Models;
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class CustomAuthorizeRequestValidator : ICustomAuthorizeRequestValidator
    {
        private readonly ILogger<CustomAuthorizeRequestValidator> _logger;
        private readonly CustomJwtRequestValidator _customJwtRequestValidator;
        private readonly IEventService _eventService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfigurationSettings _configurationSettings;
        private readonly IStatusRepository _statusRepository;

        private readonly IdentityServerOptions _options;

        private CustomAuthorizeRequestValidationContext _requestContext;
        private ValidatedAuthorizeRequest _validatedAuthoriseRequest;
        private IConfiguration _configuration;
        private int _sharingDuration;
        private string _cdrArrangementId;
        private readonly ResponseTypeEqualityComparer
           _responseTypeEqualityComparer = new ResponseTypeEqualityComparer();

        public CustomAuthorizeRequestValidator(
            ILogger<CustomAuthorizeRequestValidator> logger,
            CustomJwtRequestValidator customJwtRequestValidator,
            IHttpContextAccessor httpContextAccessor,
            IEventService eventService,
            IConfiguration configuration,
            IConfigurationSettings configurationSettings,
            IStatusRepository statusRepository,
            IdentityServerOptions options)
        {
            _logger = logger;
            _customJwtRequestValidator = customJwtRequestValidator;
            _eventService = eventService;
            _httpContextAccessor = httpContextAccessor;
            _configurationSettings = configurationSettings;
            _configuration = configuration;
            _statusRepository = statusRepository;
            _options = options;
        }

        public async Task ValidateAsync(CustomAuthorizeRequestValidationContext context)
        {
            _requestContext = context;
            _validatedAuthoriseRequest = _requestContext.Result.ValidatedRequest;

            if (await ValidAuthorizeRequest())
            {
                ConsentAndAuthenticationHelper.SetClaimsPrincipalAndForceConsentForAuthoriseRequest(_validatedAuthoriseRequest, _sharingDuration, _cdrArrangementId, _configuration, _configurationSettings, _httpContextAccessor);
            }
        }

        private async Task<bool> ValidAuthorizeRequest()
        {
            // Must have a request or request_uri parameter.
            var request = _validatedAuthoriseRequest.Raw[CdsConstants.AuthorizeRequest.Request];
            var requestUri = _validatedAuthoriseRequest.Raw[CdsConstants.AuthorizeRequest.RequestUri];

            if (string.IsNullOrEmpty(request) && string.IsNullOrEmpty(requestUri))
            {
                await SetFailedResult(ValidationCheck.AuthorizeRequestInvalid, "invalid_request");
                return false;
            }

            // Determine if this is a by ref (request_uri) or by value (request).
            // The Energy DH only accepts by reference (PAR) authorisation requests.
            if (_configuration.FapiComplianceLevel() >= FapiComplianceLevel.Fapi1Phase2 && !IsByReference(request))
            {
                await SetFailedResult(ValidationCheck.AuthorizeRequestInvalid, "invalid_request");
                return false;
            }

            // Custom request JWT Validations
            var jwtRequestResult = await ReadJwtRequestAsync(_validatedAuthoriseRequest);
            if (jwtRequestResult.IsError)
            {
                await SetFailedResult(ValidationCheck.RequestParamInvalid, "invalid_request_object");
                return false;
            }

            if (!ValidClientId())
            {
                return await SetFailedResult(ValidationCheck.ClientIdInvalid);
            }

            if (!ValidNonceInRequest())
            {
                return await SetFailedResult(ValidationCheck.ClientIdInvalid);
            }

            if (!ValidScope())
            {
                return await SetFailedResult(ValidationCheck.ScopeInvalid);
            }

            if (!ValidResponseType())
            {
                return await SetFailedResult(ValidationCheck.ResponseTypeInvalid);
            }

            if (!await ValidSoftwareProductStatus())
            {
                return await SetFailedResult(ValidationCheck.SoftwareProductStatusInvalid);
            }

            if (_configuration.FapiComplianceLevel() >= FapiComplianceLevel.Fapi1Phase2 && !ValidPkce())
            {
                return await SetFailedResult(ValidationCheck.AuthorisationRequestMissingPkce);
            }

            var claimsError = ValidClaims();
            if (claimsError.HasValue)
            {
                return await SetFailedResult(claimsError.Value);
            }

            if (!ValidateRedirectUri())
            {
                return await SetFailedResult(ValidationCheck.AuthorizeRequestInvalidRedirectUri);
            }

            return true;
        }

        private void LogError(string message, ValidatedAuthorizeRequest request)
        {
            var requestDetails = new AuthorizationRequestValidationLog(request);
            _logger.LogError("{message}\n{requestDetails}", message, requestDetails);
        }

        private static AuthorizeRequestValidationResult Invalid(ValidatedAuthorizeRequest request, string error = OidcConstants.AuthorizeErrors.InvalidRequest, string description = null)
        {
            return new AuthorizeRequestValidationResult(request, error, description);
        }
        private static AuthorizeRequestValidationResult Valid(ValidatedAuthorizeRequest request)
        {
            return new AuthorizeRequestValidationResult(request);
        }

        private async Task<AuthorizeRequestValidationResult> ReadJwtRequestAsync(ValidatedAuthorizeRequest request)
        {
            if (request.RequestObject.IsPresent())
            {
                // validate the request JWT for this client
                var jwtRequestValidationResult = await _customJwtRequestValidator.ValidateAsync(request.Client, request.RequestObject);
                if (jwtRequestValidationResult.IsError)
                {
                    LogError("request JWT validation failure", request);
                    return Invalid(request, description: "Invalid JWT request");
                }

                // Fix for .NET 6 - the claims claim is not included in the RequestObjectValues dictionary so we add in manually here.
                if (jwtRequestValidationResult.Payload.ContainsKey("claims")
                    && !request.RequestObjectValues.ContainsKey("claims"))
                {
                    request.RequestObjectValues.Add("claims", jwtRequestValidationResult.Payload["claims"]);
                }
            }

            // Remove any other prompt and add the consent prompt
            request.Raw.Set(OidcConstants.AuthorizeRequest.Prompt, OidcConstants.PromptModes.Consent);

            return Valid(request);
        }

        /// <summary>
        /// Check Client Id
        /// [CDS: #request-object]
        /// Client Id is required. IS4 default validation checks the value before here.
        /// </summary>
        private bool ValidClientId()
        {
            if (!_validatedAuthoriseRequest.RequestObjectValues.Any(x => x.Key == "client_id"))
            {
                _logger.LogError("Client Id in Request JWT is invalid or missing");
                return false;
            }

            return true;
        }

        ///
        /// Validate nonce in request
        /// fapi1-advanced-final-ensure-request-object-without-nonce-fails
        ///This test should end with the authorization server showing an error message that the request 
        ///or request object is invalid 
        /// invalid_request error (due to the missing nonce). 
        /// nonce is required for all flows that return an id_token from the authorization endpoint, 
        ///see https://openid.net/specs/openid-connect-core-1_0.html#HybridIDToken 
        ///and https://bitbucket.org/openid/connect/issues/972/nonce-requirement-in-hybrid-auth-request*/
        ///
        private bool ValidNonceInRequest()
        {
            // State should be from the request object not the query params
            var isState = _validatedAuthoriseRequest.RequestObjectValues.Any(x => x.Key == "state");
            if (!isState)
            {
                _validatedAuthoriseRequest.State = string.Empty;
            }

            if (!_validatedAuthoriseRequest.RequestObjectValues.Any(x => x.Key == "nonce"))
            {
                _logger.LogError("nonce is missing from request object");
                return false;
            }

            return true;
        }

        // Validate the redirect uri inside the request object. redirect_uri must be present, and a valid uri
        // Additional Info: fapi1-advanced-final-ensure-request-object-without-redirect-uri-fails
        private bool ValidateRedirectUri()
        {
            var redirectUri = _validatedAuthoriseRequest.RequestObjectValues.FirstOrDefault(x => x.Key == OidcConstants.TokenRequest.RedirectUri).Value;
            if (redirectUri.IsMissingOrTooLong(_options.InputLengthRestrictions.RedirectUri))
            {
                return false;
            }
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the authorisation request is based on a PAR request_uri or not.
        /// </summary>
        /// <returns>
        /// True if the authorisation has been initiated via PAR (request_uri).
        /// </returns>
        /// <remarks>
        /// There is some complexity in this determination based on the re-used of this validator in the 
        /// PushedAuthorizationRequestValidator class.
        /// When calling this validator directly from the authorisation endpoint, a check of the incoming parameters
        /// can be performed, namely request_uri (PAR) and request (non-PAR).
        /// However, when this validator is called from the PAR endpoint, the request parameter is used.  Therefore, an
        /// additional check is included -> RequestObject != null.
        /// For PAR endpoint the request parameter is used, but the RequestObject is null.
        /// For the Authorisation endpoint for non-PAR request, the request parameter is set and the RequestObject is not null.
        /// For the Authorisation endpoint for PAR request, the request_uri parameter is set and the RequestObject is not null.
        /// </remarks>
        private bool IsByReference(string request)
        {
            if (!string.IsNullOrEmpty(request) && _validatedAuthoriseRequest.RequestObject != null)
            {
                return false;
            }

            return true;
        }

        // Validate that the authorisation request contains PKCE parameters
        private bool ValidPkce()
        {
            var requestUri = _validatedAuthoriseRequest.Raw[CdsConstants.AuthorizeRequest.RequestUri];
            if (!string.IsNullOrEmpty(requestUri) && !_validatedAuthoriseRequest.IsPkce())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check Claims
        /// [CDS: #consent].
        /// </summary>
        private ValidationCheck? ValidClaims()
        {
            if (!_validatedAuthoriseRequest.RequestObjectValues.Any(x => x.Key == "claims"))
            {
                _logger.LogError("Claims in Request JWT is invalid or missing");
                return ValidationCheck.ClaimsInvalid;
            }

            var claims = _validatedAuthoriseRequest.RequestObjectValues.First(x => x.Key == "claims");

            // Check Claims
            // [CDS: #request-object]
            AuthorizeClaims authorizeClaims;

            try
            {
                authorizeClaims = JsonConvert.DeserializeObject<AuthorizeClaims>(claims.Value);

                if (!authorizeClaims.SharingDuration.HasValue)
                {
                    authorizeClaims.SharingDuration = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Claims {Claims} in Request JWT could not deserialize.", claims.Value);
                return ValidationCheck.ClaimsInvalid;
            }

            // Check sharing duration
            if (authorizeClaims.SharingDuration.Value < 0)
            {
                _logger.LogError("Sharing Duration {SharingDuration} in Request JWT is invalid, is less then 0", authorizeClaims.SharingDuration);
                return ValidationCheck.SharingDurationInvalid;
            }

            // Check sharing duration is not greater than 1 year.
            if (authorizeClaims.SharingDuration.Value > TimingsInSeconds.OneYear)
            {
                _logger.LogInformation("Sharing Duration ({sharingDuration}) in Request JWT is greater than one year {oneYear}.  Setting to {oneYear}.", authorizeClaims.SharingDuration.Value, TimingsInSeconds.OneYear, TimingsInSeconds.OneYear);
                _sharingDuration = TimingsInSeconds.OneYear;
            }
            else
            {
                _sharingDuration = authorizeClaims.SharingDuration.Value;
            }

            // Check CDR Arrangement ID
            if (!string.IsNullOrEmpty(authorizeClaims.CdrArrangementId))
            {
                if (!Guid.TryParse(authorizeClaims.CdrArrangementId, out _))
                {
                    _logger.LogError("CDR Arrangement ID is invalid format.", authorizeClaims.SharingDuration);
                    return ValidationCheck.PARInvalidCdrArrangementId;
                }

                _cdrArrangementId = authorizeClaims.CdrArrangementId;
            }

            // Check ACR
            // [CDS: #levels-of-assurance-loas]
            // Must be urn:cds.au:cdr:2 or urn:cds.au:cdr:3
            var acrValues = new List<string>();
            if (!string.IsNullOrEmpty(authorizeClaims.IdToken.Acr.Value))
            {
                // Single acr value provided.
                acrValues.Add(authorizeClaims.IdToken.Acr.Value);
            }
            else
            {
                // Array of acr values provided.
                acrValues = authorizeClaims.IdToken.Acr.Values.ToList();
            }

            if (acrValues.Count == 0 || !acrValues.All(x => x == StandardClaims.ACR2Value || x == StandardClaims.ACR3Value))
            {
                _logger.LogError("Acr {Acr} in Request JWT is invalid.", authorizeClaims.IdToken.Acr.Values.ToSpaceSeparatedString());
                return ValidationCheck.ACRInvalid;
            }

            return null;
        }

        /// <summary>
        /// Check Scope
        /// [CDS: #consent]
        /// Must be set to at least "openid", could be "openid profile"
        /// </summary>
        private bool ValidScope()
        {
            if (!_validatedAuthoriseRequest.RequestObjectValues.Any(x => x.Key == "scope")
                || !_validatedAuthoriseRequest.RequestObjectValues.First(x => x.Key == "scope").Value.Split(" ").Contains("openid"))
            {
                _logger.LogError("Scope in Request JWT is invalid or missing");
                return false;
            }

            if (!_validatedAuthoriseRequest.RequestedScopes.Contains("openid"))
            {
                _logger.LogError("Scopes {@Scopes} in Request JWT is invalid", _validatedAuthoriseRequest.RequestedScopes);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check Response Type
        /// [CDS: #authentication-flows]
        /// Only a response_type (see section 3 of [OIDC]) of code id_token SHALL be allowed.
        /// </summary>
        private bool ValidResponseType()
        {
            if (!_validatedAuthoriseRequest.RequestObjectValues.Any(x => x.Key == "response_type") || !_validatedAuthoriseRequest.RequestObjectValues.First(x => x.Key == "response_type").Value.Contains("code id_token", StringComparison.Ordinal))
            {
                _logger.LogError("Response Type in Request JWT is invalid or missing");
                return false;
            }

            if (!SupportedResponseTypes.Contains(_validatedAuthoriseRequest.ResponseType, _responseTypeEqualityComparer))
            {
                _logger.LogError("Response Type {ResponseType} in Request JWT is invalid or missing", _validatedAuthoriseRequest.ResponseType);
                return false;
            }

            return true;
        }

        private async Task<bool> ValidSoftwareProductStatus()
        {
            var claimSoftwareProduct = _validatedAuthoriseRequest.ClientClaims.FirstOrDefault(c => c.Type == "software_id");

            if (claimSoftwareProduct == null)
            {
                _logger.LogError("Software Id in Request JWT is invalid or missing");
                return false;
            }

            //Data recipient software product
            var softwareProductId = claimSoftwareProduct?.Value;
            var softwareProduct = await _statusRepository.GetSoftwareProduct(Guid.Parse(softwareProductId));

            if (softwareProduct == null)
            {
                _logger.LogError("Software Product is not available");
                return false;
            }

            if (String.Equals(softwareProduct.Status, "Removed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Software Product Status in Request JWT is Removed");
                return false;
            }
            else if (String.Equals(softwareProduct.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Software Product Status in Request JWT is Inactive");
                return false;
            }

            return true;
        }

        private async Task<bool> SetFailedResult(ValidationCheck validationCheck, string error = AuthorizeErrorCodes.InvalidRequest)
        {
            await _eventService.RaiseAsync(new RequestValidationFailureEvent(validationCheck));

            // OIDC 3.1.2.6 defines the Error Codes allowed to return
            _requestContext.Result = new AuthorizeRequestValidationResult(_requestContext.Result.ValidatedRequest)
            {
                IsError = true,
                Error = error,
                ErrorDescription = validationCheck.GetDescription(),
            };
            return false;
        }
    }
}
