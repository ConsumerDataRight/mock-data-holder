using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using GrantTypes = CDR.DataHolder.IdentityServer.CdsConstants.GrantTypes;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class PushedAuthorizationRequestService : IPushedAuthorizationRequestService
    {
        private readonly ILogger _logger;
        private readonly IEventService _eventService;
        private readonly IPersistedGrantStore _persistedGrantStore;
        private readonly IPushedAuthorizationRequestValidator _validator;
        private readonly IConfigurationSettings _configurationSettings;

        public PushedAuthorizationRequestService(
            ILogger<PushedAuthorizationRequestService> logger,
            IPersistedGrantStore persistedGrantStore,
            IPushedAuthorizationRequestValidator validator,
            IEventService eventService,
            IConfigurationSettings configurationSettings)
        {
            _persistedGrantStore = persistedGrantStore;
            _eventService = eventService;
            _logger = logger;
            _validator = validator;
            _configurationSettings = configurationSettings;
        }

        public async Task<PushedAuthorizationResult> ProcessAuthoriseRequest(NameValueCollection parameters)
        {
            var authorizeValidatorResult = await _validator.ValidateAsync(parameters);

            var parResultResponse = new PushedAuthorizationResult();

            if (!authorizeValidatorResult.IsError)
            {
                // Build the URI to the request reference object.
                var requestRef = Guid.NewGuid().ToString();
                parResultResponse.RequestUri = $"{AuthorizeRequest.RequestUriPrefix}{requestRef}";

                // Store the authorisation request as a persisted grant that can be retrieved within 90 seconds.
                await _persistedGrantStore.StoreAsync(new PersistedGrant()
                {
                    ClientId = authorizeValidatorResult.ValidatedRequest.ClientId,
                    CreationTime = DateTime.UtcNow,
                    Data = parameters["request"],
                    Expiration = DateTime.UtcNow.AddSeconds(_configurationSettings.ParRequestUriExpirySeconds),
                    Key = requestRef,
                    SubjectId = "",
                    Type = GrantTypes.PushAuthoriseRequest,
                });
            }
            else
            {
                _logger.LogError("Authorize validator returned error {error} {errordescription}", authorizeValidatorResult.Error, authorizeValidatorResult.ErrorDescription);
                switch (authorizeValidatorResult.Error)
                {
                    case PushedAuthorizationServiceErrorCodes.InvalidCdrArrangementId:
                        await _eventService.RaiseAsync(new PushedAuthorizationRequestValidationFailureEvent(ValidationCheck.PARInvalidCdrArrangementId));
                        break;
                    case PushedAuthorizationServiceErrorCodes.RequestJwtFailedValidation:
                        await _eventService.RaiseAsync(new PushedAuthorizationRequestValidationFailureEvent(ValidationCheck.PARRequestJwtFailedValidation));
                        break;
                    case PushedAuthorizationServiceErrorCodes.UnauthorizedClient:
                        await _eventService.RaiseAsync(new PushedAuthorizationRequestValidationFailureEvent(ValidationCheck.PARRequestInvalidClient));
                        break;
                    default:
                        await _eventService.RaiseAsync(new PushedAuthorizationRequestValidationFailureEvent(ValidationCheck.PARInvalidRequest));
                        break;
                }

                parResultResponse.ErrorDescription = authorizeValidatorResult.ErrorDescription;
                parResultResponse.Error = authorizeValidatorResult.Error;
            }

            return parResultResponse;
        }
    }
}
