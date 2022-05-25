using System;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Models;
using FluentValidation;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Validation.Messages.ClientRevocationRequestMessages;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class ClientRevocationRequestValidator : AbstractValidator<ClientRevocationRequest>
    {
        private readonly IEventService _eventService;
        private readonly ILogger<ClientRevocationRequestValidator> _logger;
        private readonly IValidator<ClientDetails> _clientDetailsValidator;
        private readonly IValidator<MtlsCredential> _mtlsCredentialValidator;

        public ClientRevocationRequestValidator(
            IValidator<ClientDetails> clientDetailsValidator,
            IValidator<MtlsCredential> mtlsCredentialValidator,
            IEventService eventService,
            ILogger<ClientRevocationRequestValidator> logger)
        {
            _eventService = eventService;
            _logger = logger;
            _clientDetailsValidator = clientDetailsValidator;
            _mtlsCredentialValidator = mtlsCredentialValidator;

            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage(MissingTokenParameter)
                .OnFailure(RaiseTokenIsMissing)
                .DependentRules(MtlsValidation);
        }

        private void MtlsValidation()
        {
            RuleFor(x => x.MtlsCredential)
                .NotNull()
                .WithMessage(MissingMtlsCredentials)
                .SetValidator(_mtlsCredentialValidator)
                .DependentRules(ClientDetailsValidation);
        }

        private void ClientDetailsValidation()
        {
            RuleFor(x => x.ClientDetails)
                .NotNull()
                .WithMessage(MissingClientDetails)
                .SetValidator(_clientDetailsValidator);
        }

        private Action<ClientRevocationRequest> RaiseTokenIsMissing
            => RaiseRevocationRequestValidationFailureEvent(ValidationCheck.RevocationRequestInvalidParameters, MissingTokenParameter);

        private Action<ClientRevocationRequest> RaiseRevocationRequestValidationFailureEvent(ValidationCheck check, string message)
            => _ =>
            {
                _logger.LogError("{message}", message);
                _eventService.RaiseAsync(new RevocationRequestValidationFailureEvent(check)).GetAwaiter().GetResult();
            };
    }
}
