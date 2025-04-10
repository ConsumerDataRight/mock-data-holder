﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace CDR.DataHolder.Shared.API.Infrastructure.Authorization
{
    public class MtlsHandler : AuthorizationHandler<MtlsRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<MtlsHandler> _logger;

        public MtlsHandler(IHttpContextAccessor httpContextAccessor, ILogger<MtlsHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MtlsRequirement requirement)
        {
            // Check that authentication was successful before doing anything else
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return Task.CompletedTask;
            }

            // Check that the thumprint of the client cert used for TLS MA is the same
            // as the one expected by the cnf:x5t#S256 claim in the access token.
            string? requestHeaderClientCertThumprint = null;
            if (_httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("X-TlsClientCertThumbprint", out StringValues headerThumbprints))
            {
                requestHeaderClientCertThumprint = headerThumbprints[0];
            }

            _logger.LogDebug("{Class}.{Method} - X-TlsClientCertThumbprint: {Thumbprint}", nameof(MtlsHandler), nameof(HandleRequirementAsync), headerThumbprints);

            if (string.IsNullOrWhiteSpace(requestHeaderClientCertThumprint))
            {
                _logger.LogError("Unauthorized request. Request header 'X-TlsClientCertThumbprint' is missing.");
                return Task.CompletedTask;
            }

            string? accessTokenClientCertThumbprint = null;
            var cnfJson = context.User.FindFirst("cnf")?.Value;
            if (!string.IsNullOrWhiteSpace(cnfJson))
            {
                var cnf = JObject.Parse(cnfJson);
                accessTokenClientCertThumbprint = cnf.Value<string>("x5t#S256");
            }

            if (string.IsNullOrWhiteSpace(accessTokenClientCertThumbprint))
            {
                _logger.LogError("Unauthorized request. cnf:x5t#S256 claim is missing from access token.");
                return Task.CompletedTask;
            }

            if (!accessTokenClientCertThumbprint.Equals(requestHeaderClientCertThumprint, System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Unauthorized request. X-TlsClientCertThumbprint request header value '{RequestHeaderClientCertThumprint}' does not match access token cnf:x5t#S256 claim value '{AccessTokenClientCertThumbprint}'", requestHeaderClientCertThumprint, accessTokenClientCertThumbprint);
                return Task.CompletedTask;
            }

            // If we get this far all good
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
