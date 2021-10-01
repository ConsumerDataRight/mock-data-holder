using System.Collections.Generic;

using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class ClaimsService : DefaultClaimsService
    {
        public ClaimsService(IProfileService profile, ILogger<DefaultClaimsService> logger)
            : base(profile, logger)
        {
        }

        /// <summary>
        /// Gets additional (and optional) claims from the cookie or incoming subject.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <returns>Additional claims.</returns>
        protected override IEnumerable<Claim> GetOptionalClaims(ClaimsPrincipal subject)
        {
            var claims = new List<Claim>();

            var acr = subject.FindFirst(JwtClaimTypes.AuthenticationContextClassReference);
            if (acr != null)
            {
                claims.Add(acr);
            }

            var sharingExpiresAt = subject.FindFirst(StandardClaims.SharingDurationExpiresAt);
            if (sharingExpiresAt != null)
            {
                claims.Add(sharingExpiresAt);
            }

            var cdrArrangementId = subject.FindFirst(StandardClaims.CDRArrangementId);
            if (cdrArrangementId != null)
            {
                claims.Add(cdrArrangementId);
            }

            return claims;
        }
    }
}