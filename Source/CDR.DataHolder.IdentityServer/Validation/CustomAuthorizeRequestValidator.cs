using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Helpers;
using CDR.DataHolder.IdentityServer.Logging;
using CDR.DataHolder.IdentityServer.Models;
using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
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

        private CustomAuthorizeRequestValidationContext _requestContext;
        private ValidatedAuthorizeRequest _validatedAuthoriseRequest;
        private HttpRequest _httpRequest;
        private int _sharingDuration;
        private string _cdrArrangementId;
        private readonly ResponseTypeEqualityComparer
           _responseTypeEqualityComparer = new ResponseTypeEqualityComparer();

        public CustomAuthorizeRequestValidator(
            ILogger<CustomAuthorizeRequestValidator> logger,
            CustomJwtRequestValidator customJwtRequestValidator,
            IHttpContextAccessor httpContextAccessor,
            IEventService eventService,
            IConfigurationSettings configurationSettings, 
            IStatusRepository statusRepository)
        {
            _logger = logger;
            _customJwtRequestValidator = customJwtRequestValidator;
            _eventService = eventService;
            _httpContextAccessor = httpContextAccessor;
            _configurationSettings = configurationSettings;            
            _statusRepository = statusRepository;
        }

        public async Task ValidateAsync(CustomAuthorizeRequestValidationContext context)
        {
            _requestContext = context;
            _validatedAuthoriseRequest = _requestContext.Result.ValidatedRequest;
            _httpRequest = _httpContextAccessor.HttpContext.Request;

            if (await ValidAuthorizeRequest())
            {
                ConsentAndAuthenticationHelper.SetClaimsPrincipalAndForceConsentForAuthoriseRequest(_validatedAuthoriseRequest, _sharingDuration, _cdrArrangementId, _configurationSettings, _httpContextAccessor);
            }
        }

        private async Task<bool> ValidAuthorizeRequest()
        {
            // Custom request JWT Validations
            var jwtRequestResult = await ReadJwtRequestAsync(_validatedAuthoriseRequest);
            if (jwtRequestResult.IsError)
            {
                await SetFailedResult(ValidationCheck.RequestParamInvalid);
                return false;
            }

            if (!ValidClientId())
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
                return await SetFailedResult(ValidationCheck.SoftwreProductStatusInvalid);
            }

            var claimsError = ValidClaims();
            if (claimsError.HasValue)
            {
                return await SetFailedResult(claimsError.Value);
            }

            return true;
        }

        private void LogError(string message, ValidatedAuthorizeRequest request)
        {
            var requestDetails = new AuthorizationRequestValidationLog(request);
            _logger.LogError(message + "\n{@requestDetails}", requestDetails);
        }

        private void LogError(string message, string detail, ValidatedAuthorizeRequest request)
        {
            var requestDetails = new AuthorizationRequestValidationLog(request);
            _logger.LogError(message + ": {detail}\n{@requestDetails}", detail, requestDetails);
        }
        private AuthorizeRequestValidationResult Invalid(ValidatedAuthorizeRequest request, string error = OidcConstants.AuthorizeErrors.InvalidRequest, string description = null)
        {
            return new AuthorizeRequestValidationResult(request, error, description);
        }
        private AuthorizeRequestValidationResult Valid(ValidatedAuthorizeRequest request)
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
            else
            {
                // Check sharing duration is not greater than 1 year.
                if (authorizeClaims.SharingDuration.Value > TimingsInSeconds.OneYear)
                {
                    _logger.LogInformation($"Sharing Duration ({authorizeClaims.SharingDuration.Value}) in Request JWT is greater than one year {TimingsInSeconds.OneYear}.  Setting to {TimingsInSeconds.OneYear}.");
                    _sharingDuration = TimingsInSeconds.OneYear;
                }
                else
                {
                    _sharingDuration = authorizeClaims.SharingDuration.Value;
                }
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
            var claimSoftwareProduct = _validatedAuthoriseRequest.ClientClaims.Where(c => c.Type == "software_id").FirstOrDefault();

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

        private async Task<bool> SetFailedResult(ValidationCheck validationCheck)
        {
            await _eventService.RaiseAsync(new RequestValidationFailureEvent(validationCheck));

            // OIDC 3.1.2.6 defines the Error Codes allowed to return
            _requestContext.Result = new AuthorizeRequestValidationResult(_requestContext.Result.ValidatedRequest)
            {
                IsError = true,
                Error = AuthorizeErrorCodes.InvalidRequest,
                ErrorDescription = validationCheck.GetDescription(),
            };
            return false;
        }
    }
}
