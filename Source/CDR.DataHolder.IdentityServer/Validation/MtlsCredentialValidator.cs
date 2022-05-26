using System;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Models;
using FluentValidation;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Validation.Messages.MtlsCredentialMessages;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class MtlsCredentialValidator : AbstractValidator<MtlsCredential>
    {
        private readonly IEventService _eventService;
        private readonly ILogger _logger;

        public MtlsCredentialValidator(IEventService eventService, ILogger<MtlsCredentialValidator> logger)
        {
            _eventService = eventService;
            _logger = logger;

            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.CertificateCommonName)
                .NotEmpty()
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseCertificateCommonNameHeaderNotFound)
                .DependentRules(() =>
                {
                    RuleFor(x => x.CertificateThumbprint)
                        .NotEmpty()
                        .WithMessage(TokenErrors.InvalidClient)
                        .OnFailure(RaiseCertificateThumbprintHeaderNotFound);
                });
        }

        private Action<MtlsCredential> RaiseCertificateCommonNameHeaderNotFound
            => RaiseEvent(ValidationCheck.SSLClientCertCNNotFound, ClientCertificateCommonNameMissing);

        private Action<MtlsCredential> RaiseCertificateThumbprintHeaderNotFound
            => RaiseEvent(ValidationCheck.SSLClientCertThumbprintMissing, ClientCertificateThumbprintMissing);

        private Action<MtlsCredential> RaiseEvent(ValidationCheck check, string message)
            => _ =>
            {
                _logger.LogError("{message}", message);
                _eventService.RaiseAsync(new MtlsValidationFailureEvent(check)).GetAwaiter().GetResult();
            };
    }
}