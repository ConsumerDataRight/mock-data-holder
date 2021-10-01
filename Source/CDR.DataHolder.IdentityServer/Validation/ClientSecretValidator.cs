using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Models;
using FluentValidation;
using FluentValidation.Results;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using static IdentityServer4.IdentityServerConstants;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class ClientSecretValidator : ISecretValidator
    {
        private readonly IValidator<ClientTokenRequest> _clientTokenRequestValidator;
        private readonly IValidator<ClientRevocationRequest> _clientRevocationRequestValidator;
        private readonly IValidator<ClientArrangementRevocationRequest> _clientArrangementRevocationRequestValidator;
        private readonly IEventService _eventService;

        public ClientSecretValidator(
            IValidator<ClientTokenRequest> clientTokenRequestValidator,
            IValidator<ClientRevocationRequest> clientRevocationRequestValidator,
            IValidator<ClientArrangementRevocationRequest> clientArrangementRevocationRequestValidator,
            IEventService eventService)
        {
            _clientTokenRequestValidator = clientTokenRequestValidator;
            _clientRevocationRequestValidator = clientRevocationRequestValidator;
            _clientArrangementRevocationRequestValidator = clientArrangementRevocationRequestValidator;
            _eventService = eventService;
        }

        public async Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
        {
            return parsedSecret.Type switch
            {
                CdsConstants.ParsedSecretTypes.ArrangementRevocationSecret => await ValidateArrangementRevocationRequest(secrets, parsedSecret),
                CdsConstants.ParsedSecretTypes.TokenSecret => await ValidateTokenRequest(secrets, parsedSecret),
                CdsConstants.ParsedSecretTypes.RevocationSecret => await ValidateRevocationRequest(secrets, parsedSecret),
                _ => Error("invalid request type"),
            };
        }

        private Task<SecretValidationResult> ValidateTokenRequest(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
            => ValidateRequest(secrets, parsedSecret, _clientTokenRequestValidator, OnTokenRequestValidationSuccess, ProcessTokenValidationErrors);

        private Task<SecretValidationResult> ValidateRevocationRequest(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
            => ValidateRequest(secrets, parsedSecret, _clientRevocationRequestValidator, _ => Success(), ProcessRevocationValidationErrors);

        private Task<SecretValidationResult> ValidateArrangementRevocationRequest(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
            => ValidateRequest(secrets, parsedSecret, _clientArrangementRevocationRequestValidator, _ => Success(), ProcessArrangementRevocationValidationErrors);

        private async Task<SecretValidationResult> ValidateRequest<T>(
            IEnumerable<Secret> secrets,
            ParsedSecret parsedSecret,
            IValidator<T> validator,
            Func<T, SecretValidationResult> onSuccess,
            Func<IList<ValidationFailure>, Task> onError)
            where T : ClientRequest
        {
            var clientRequest = parsedSecret.Credential as T;
            if (clientRequest?.ClientDetails != null)
            {
                clientRequest.ClientDetails.TrustedKeys = secrets
                    .Where(s => s.Type == SecretTypes.JsonWebKey)
                    .Select(s => new Microsoft.IdentityModel.Tokens.JsonWebKey(s.Value))
                    .ToArray();
            }

            var validationResult = validator.Validate(clientRequest);

            if (validationResult.IsValid)
            {
                await RaiseSuccessEvents();
                return onSuccess(clientRequest);
            }

            await onError(validationResult.Errors);
            return Error(validationResult.Errors.Select(x => x.ErrorMessage).FirstOrDefault());
        }

        private static SecretValidationResult OnTokenRequestValidationSuccess(ClientTokenRequest tokenRequest)
        {
            // Certificate validation passed so add the thumbprint of the certificate to the access token.
            var confirmation = new Dictionary<string, string>
            {
                { "x5t#S256", tokenRequest.MtlsCredential.CertificateThumbprint },
            };
            return Success(confirmation);
        }

        private static SecretValidationResult Success(Dictionary<string, string> confirmation = null)
            => new SecretValidationResult
            {
                Success = true,
                Confirmation = confirmation?.ToJson(),
                IsError = false,
            };

        private static SecretValidationResult Error(string error)
            => new SecretValidationResult
            {
                Success = false,
                IsError = true,
                Error = error,
            };

        private async Task RaiseSuccessEvents()
        {
            // Not raising token request success event as the validation of the token has not be finished at this stage. We rely on IDS4 to raise that event for us.
            await _eventService.RaiseAsync(new MtlsValidationSuccessEvent());
            await _eventService.RaiseAsync(new ClientAssertionSuccessEvent());
        }

        private async Task ProcessTokenValidationErrors(IList<ValidationFailure> errors)
        {
            // TokenRequestValidation passed
            // Not raising token request success event as the validation of the token has not be finished at this stage. We rely on IDS4 to raise that event for us.
            await ProcessClientRequestValidationErrors(errors);
        }

        private async Task ProcessRevocationValidationErrors(IList<ValidationFailure> errors)
        {
            if (!errors.Any(x => x.PropertyName.IsAnyOf(nameof(ClientRevocationRequest.Token), nameof(ClientRevocationRequest.TokenTypeHint))))
            {
                // RevocationRequestValidation passed
                // Not raising revocation request success event as the validation of the token has not be finished at this stage. We rely on IDS4 to raise that event for us.
                await ProcessClientRequestValidationErrors(errors);
            }
        }

        private async Task ProcessArrangementRevocationValidationErrors(IList<ValidationFailure> errors)
        {
            await ProcessClientRequestValidationErrors(errors);
        }

        private async Task ProcessClientRequestValidationErrors(IList<ValidationFailure> errors)
        {
            if (!errors.Any(x => x.PropertyName.StartsWith(nameof(ClientRequest.MtlsCredential), StringComparison.CurrentCulture)))
            {
                // MTLS was valid
                await _eventService.RaiseAsync(new MtlsValidationSuccessEvent());

                if (!errors.Any(x => x.PropertyName.StartsWith(nameof(ClientRequest.ClientDetails), StringComparison.CurrentCulture)))
                {
                    // client_assertion was valid
                    await _eventService.RaiseAsync(new ClientAssertionSuccessEvent());
                }
            }
        }
    }
}
