using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services;
using FluentValidation;
using FluentValidation.Validators;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using static CDR.DataHolder.IdentityServer.Validation.Messages.ClientDetailsMessages;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class ClientDetailsValidator : AbstractValidator<ClientDetails>
    {
        private readonly IConfiguration _config;
        private readonly IdentityServerOptions _options;
        private readonly IEventService _eventService;
        private readonly ILogger<ClientDetailsValidator> _logger;
        private readonly ITokenReplayCache _tokenCache;
        private readonly IClientService _clientService;

        public ClientDetailsValidator(
            IConfiguration config,
            IdentityServerOptions options,
            IEventService eventService,
            IClientService clientService,
            ILogger<ClientDetailsValidator> logger,
            ITokenReplayCache tokenCache)
        {
            _config = config;
            _options = options;
            _eventService = eventService;
            _clientService = clientService;
            _logger = logger;
            _tokenCache = tokenCache;

            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ClientAssertionType)
                .Equal(ClientAssertionTypes.JwtBearer)
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseInvalidOrMissingClientAssertionType)
                .DependentRules(ClientValidation);
        }

        private Action<ClientDetails> RaiseClientAssertionInvalidToken
            => RaiseEvent(ValidationCheck.TokenFailedValidationWithIdentityServer, InvalidClientAssertion);

        private Action<ClientDetails> RaiseClientAssertionTooLong
            => RaiseEvent(ValidationCheck.ClientAssertionExceedsLength, ClientAssertionTooLong);

        private Action<ClientDetails> RaiseClientAssertionMissing
            => RaiseEvent(ValidationCheck.ClientAssertionNotFound, ClientAssertionMissing);

        private Action<ClientDetails> RaiseNoTrustedKeysLoaded
            => RaiseEvent(ValidationCheck.NoKeysToValidateClientAssertion, TrustedKeysEmpty);

        private Action<ClientDetails> RaiseCannotParseSecret
            => RaiseEvent(ValidationCheck.CannotParseSecret, TrustedKeysMissing);

        private Action<ClientDetails> RaiseInvalidOrMissingClientAssertionType
            => RaiseEvent(ValidationCheck.ClientAssertionNotFound, InvalidClientAssertionType);

        private Action<ClientDetails> RaiseClientIdMissing
            => RaiseEvent(ValidationCheck.ClientAssertioClientIdNotFound, MissingClientId);

        private void ClientValidation()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseClientIdMissing)
                .DependentRules(TrustedKeysValidation);
        }

        private void TrustedKeysValidation()
        {
            RuleFor(x => x.TrustedKeys)
                .NotNull()
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseCannotParseSecret)
                .NotEmpty()
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseNoTrustedKeysLoaded)
                .DependentRules(ClientAssertionValidation);
        }

        private void ClientAssertionValidation()
        {
            RuleFor(x => x.ClientAssertion)
                .NotEmpty()
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseClientAssertionMissing)
                .MaximumLength(_options.InputLengthRestrictions.Jwt)
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseClientAssertionTooLong)
                .Must(BeValidJwt)
                .WithMessage(TokenErrors.InvalidClient)
                .OnFailure(RaiseClientAssertionInvalidToken);
        }

        private Action<ClientDetails> RaiseEvent(ValidationCheck check, string message)
            => _ =>
            {
                _logger.LogError("{message}", message);
                _eventService.RaiseAsync(new ClientAssertionFailureEvent(check)).GetAwaiter().GetResult();
            };

        private bool BeValidJwt(ClientDetails clientDetails, string clientAssertion)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.FromMinutes(1),
                IssuerSigningKeys = clientDetails.TrustedKeys,
                ValidateIssuerSigningKey = true,
                ValidIssuer = clientDetails.ClientId,
                ValidateIssuer = true,
                ValidAudiences = _config.GetValidAudiences(),
                ValidateAudience = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateTokenReplay = true,
                TokenReplayCache = _tokenCache,
            };

            JwtSecurityToken jwtToken;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                Task.Run(async () => await _clientService.EnsureKid(clientDetails.ClientId, clientAssertion, tokenValidationParameters)).Wait();
                handler.ValidateToken(clientAssertion, tokenValidationParameters, out var token);
                jwtToken = token as JwtSecurityToken;
            }
            catch (Exception exception)
            {
                var customMessage = exception switch
                {
                    SecurityTokenExpiredException _ => TokenExpired,
                    SecurityTokenInvalidAudienceException _ => InvalidAud,
                    SecurityTokenInvalidLifetimeException _ => InvalidNbf,
                    SecurityTokenInvalidSignatureException _ => InvalidSignature,
                    SecurityTokenNoExpirationException _ => ExpIsMissing,
                    SecurityTokenNotYetValidException _ => InvalidValidFrom,
                    SecurityTokenReplayDetectedException _ => TokenReplayed,
                    SecurityTokenInvalidIssuerException _ => InvalidIss,
                    Exception _ => ClientAssertionParseError,
                };

                _logger.LogError(exception, "{message}", customMessage);
                return false;
            }

            if (jwtToken == null)
            {
                return false;
            }

            if (jwtToken.Id.IsMissing())
            {
                _logger.LogError(JtiIsMissing);
                return false;
            }

            if (_tokenCache.TryFind(jwtToken.Id))
            {
                _logger.LogError(JtiAlreadyUsed);
                return false;
            }

            _tokenCache.TryAdd(jwtToken.Id, jwtToken.ValidTo);

            if (jwtToken.Subject.IsMissing())
            {
                _logger.LogError(SubIsMissing);
                return false;
            }

            if (jwtToken.Subject.Length > _options.InputLengthRestrictions.ClientId)
            {
                _logger.LogError(SubTooLong);
                return false;
            }

            if (jwtToken.Subject != jwtToken.Issuer || jwtToken.Subject != clientDetails.ClientId)
            {
                _logger.LogError(InvalidSub);
                return false;
            }

			// Validate the alg
			var expectedAlgs = new string[] { Algorithms.Signing.PS256, Algorithms.Signing.ES256, }; // Maybe get it from the config of the Client?
			if (jwtToken.Header?.Alg == null || !expectedAlgs.Contains(jwtToken.Header?.Alg))
			{
				_logger.LogError(InvalidAlg);
				return false;
			}

			return true;
        }

    }
}