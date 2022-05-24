using System;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Models;
using FluentValidation;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Validation.Messages.ClientTokenRequestMessages;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class ClientTokenRequestValidator : AbstractValidator<ClientTokenRequest>
    {
        private readonly IEventService _eventService;
        private readonly ILogger<ClientTokenRequestValidator> _logger;
        private readonly IValidator<ClientDetails> _clientDetailsValidator;

        public ClientTokenRequestValidator(
            IValidator<ClientDetails> clientDetailsValidator,
            IValidator<MtlsCredential> mtlsCredentialValidator,
            IEventService eventService,
            ILogger<ClientTokenRequestValidator> logger)
        {
            _eventService = eventService;
            _logger = logger;
            _clientDetailsValidator = clientDetailsValidator;

            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.MtlsCredential)
                .NotNull()
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseMtlsCredentialIsMissing)
                .SetValidator(mtlsCredentialValidator)
                .DependentRules(CliendDetailsValidation);
        }

        private void CliendDetailsValidation()
        {
            RuleFor(x => x.ClientDetails)
                .NotNull()
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseClientDetailsIsMissing)
                .SetValidator(_clientDetailsValidator);
        }

        private Action<ClientTokenRequest> RaiseMtlsCredentialIsMissing
            => RaiseTokenRequestValidationFailureEvent(ValidationCheck.TokenRequestInvalidParameters, MissingMtlsCredential);

        private Action<ClientTokenRequest> RaiseClientDetailsIsMissing
            => RaiseTokenRequestValidationFailureEvent(ValidationCheck.TokenRequestInvalidParameters, MissingClientDetails);

        private Action<ClientTokenRequest> RaiseTokenRequestValidationFailureEvent(ValidationCheck check, string message)
            => _ =>
            {
                _logger.LogError("{message}", message);
                _eventService.RaiseAsync(new TokenRequestValidationFailureEvent(check, message)).GetAwaiter().GetResult();
            };
    }
}