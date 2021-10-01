using System;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Logging;
using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Validation
{
	public class CustomTokenRequestValidator : ICustomTokenRequestValidator
	{
		private readonly ILogger<CustomTokenRequestValidator> _logger;
		private readonly IEventService _eventService;

		public CustomTokenRequestValidator(ILogger<CustomTokenRequestValidator> logger, 
			IEventService eventService)
		{
			_logger = logger;
			this._eventService = eventService;
		}

		public async Task ValidateAsync(CustomTokenRequestValidationContext context)
		{
			var requestContext = context;
			var validatedTokenRequest = context.Result.ValidatedRequest;
			var requestedScope = validatedTokenRequest.Raw["scope"];
			var requestedScopes = requestedScope != null ? requestedScope?.Split(' ') : new string[] { };

			// Validate redirect_uri. If the scope is only for registration functionalities, the redirect uri is not required.
			bool isCdrRegistration = requestedScope == "cdr:registration";
			var redirectUri = validatedTokenRequest.Raw.Get(OidcConstants.TokenRequest.RedirectUri);
			if (!isCdrRegistration)
			{
				if (redirectUri.IsMissing())
				{
					LogError(validatedTokenRequest, "Redirect URI is missing");
					await SetFailedResult(TokenErrors.InvalidRequest, ValidationCheck.TokenRequestInvalidUri, requestContext);
				}

				if (!validatedTokenRequest.Client.RedirectUris.Any(r => r.Equals(redirectUri, StringComparison.OrdinalIgnoreCase)))
				{
					LogError(validatedTokenRequest, "Invalid redirect_uri", new { redirectUri, expectedRedirectUri = validatedTokenRequest.Client.RedirectUris });
					await SetFailedResult(TokenErrors.InvalidRequest, ValidationCheck.TokenRequestInvalidUri, requestContext);
				}
			}

			// Validate scopes when requesting new token from a refresh token.
			if (!string.IsNullOrEmpty(requestedScope) && validatedTokenRequest.RefreshToken != null)
            {
				var existingScopes = validatedTokenRequest.RefreshToken.Scopes;

				// The requested scopes (RequestedScopes) cannot contain a scope that was not in the original sharing arrangement (RefreshToken.Scopes).
				if (requestedScopes.Except(existingScopes).Any())
				{
					LogError(validatedTokenRequest, "Invalid scope", new { requestedScopes = requestedScope, existingScopes = string.Join(" ", existingScopes) });
					await SetFailedResult(TokenErrors.InvalidScope, ValidationCheck.TokenRequestInvalidScope, requestContext);
				}

				validatedTokenRequest.RequestedScopes = requestedScopes;
			}
		}

		private async Task SetFailedResult(string error, ValidationCheck validationCheck, CustomTokenRequestValidationContext requestContext)
		{
			await _eventService.RaiseAsync(new TokenRequestValidationFailureEvent(validationCheck));

			// OIDC 3.1.2.6 defines the Error Codes allowed to return
			requestContext.Result = new TokenRequestValidationResult(requestContext.Result.ValidatedRequest)
			{
				IsError = true,
				Error = error,
				ErrorDescription = validationCheck.GetDescription(),
			};
		}

		private void LogError(ValidatedTokenRequest validatedTokenRequest, string message = null, object values = null)
		{
			var requestDetails = new TokenRequestValidationLog(validatedTokenRequest);
			_logger.LogError(message + "\n{@requestDetails}", requestDetails);
		}
	}
}
