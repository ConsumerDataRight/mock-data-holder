using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Events;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Validation.Messages.CustomTokenRevocationRequestMessage;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class CustomTokenRevocationRequestValidator : ITokenRevocationRequestValidator
    {
        private readonly ILogger _logger;
        private readonly IEventService _eventService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTokenRevocationRequestValidator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public CustomTokenRevocationRequestValidator(ILogger<CustomTokenRevocationRequestValidator> logger, IEventService eventService)
        {
            _logger = logger;
            _eventService = eventService;
        }

        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// parameters
        /// or
        /// client
        /// </exception>
        public async Task<TokenRevocationRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, Client client)
        {
            _logger.LogTrace("ValidateRequestAsync called");

            if (parameters == null)
            {
                _logger.LogError("no parameters passed");
                throw new ArgumentNullException(nameof(parameters));
            }

            if (client == null)
            {
                _logger.LogError("no client passed");
                throw new ArgumentNullException(nameof(client));
            }

            ////////////////////////////
            // make sure token is present
            ///////////////////////////
            var token = parameters.Get("token");
            if (token.IsMissing())
            {
                await RaiseCustomTokenRevocationRequestValidationFailureEvent(ValidationCheck.RevocationRequestNoTokenFoundInRequest, NoTokenFoundInRequest);
                return new TokenRevocationRequestValidationResult
                {
                    IsError = true,
                    Error = OidcConstants.TokenErrors.InvalidRequest,
                };
            }

            var result = new TokenRevocationRequestValidationResult
            {
                IsError = false,
                Token = token,
                Client = client,
            };

            ////////////////////////////
            // check token type hint
            ///////////////////////////
            var hint = parameters.Get("token_type_hint");
            if (hint.IsPresent() && CdsConstants.SupportedTokenTypeHints.Contains(hint))
            {
                // In identity server default revocation endpoint, we can only revoke refresh token and reference access token.
                // In current token endpoint implementation, the access token generated is self-contain, which is not stored in grant store, will expire in 2 - 10 mins.
                // So in current revocation mechanism, revocation will only revoke refresh_token.
                _logger.LogDebug("Token type hint found in request: {TokenTypeHint}", hint);
                result.TokenTypeHint = hint;
            }

            _logger.LogDebug("ValidateRequestAsync result: {@ValidateRequestResult}", result);

            return result;
        }
        
        private async Task RaiseCustomTokenRevocationRequestValidationFailureEvent(ValidationCheck check, string message)
        {
            _logger.LogError("{message}", message);
            await _eventService.RaiseAsync(new RevocationRequestValidationFailureEvent(check));
        }
    }
}
