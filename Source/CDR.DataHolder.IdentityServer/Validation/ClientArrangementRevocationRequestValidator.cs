using System;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Models;
using FluentValidation;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Validation.Messages.ClientArrangementRevocationRequestMessages;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class ClientArrangementRevocationRequestValidator : AbstractValidator<ClientArrangementRevocationRequest>
    {
        private readonly IEventService _eventService;
        private readonly ILogger<ClientArrangementRevocationRequestValidator> _logger;
        private readonly IValidator<ClientDetails> _clientDetailsValidator;
        private readonly IValidator<MtlsCredential> _mtlsCredentialValidator;

        public ClientArrangementRevocationRequestValidator(
            IValidator<ClientDetails> clientDetailsValidator,
            IValidator<MtlsCredential> mtlsCredentialValidator,
            IEventService eventService,
            ILogger<ClientArrangementRevocationRequestValidator> logger)
        {
            _eventService = eventService;
            _logger = logger;
            _clientDetailsValidator = clientDetailsValidator;
            _mtlsCredentialValidator = mtlsCredentialValidator;

            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.CdrArrangementId)
                .NotEmpty()
                .WithMessage(MissingCdrArrangementIdParameter)
                .OnFailure(RaiseCdrArrangementIdIsMissing)
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

        private Action<ClientArrangementRevocationRequest> RaiseCdrArrangementIdIsMissing
            => RaiseArrangementRevocationRequestValidationFailureEvent(ValidationCheck.CdrArrangementRevocationInvalidCDRArrangementId, MissingCdrArrangementIdParameter);

        private Action<ClientArrangementRevocationRequest> RaiseArrangementRevocationRequestValidationFailureEvent(ValidationCheck check, string message)
            => _ =>
            {
                _logger.LogError("{message}", message);
                _eventService.RaiseAsync(new CdrArrangementRevocationValidationFailureEvent(check)).GetAwaiter().GetResult();
            };
    }
}
