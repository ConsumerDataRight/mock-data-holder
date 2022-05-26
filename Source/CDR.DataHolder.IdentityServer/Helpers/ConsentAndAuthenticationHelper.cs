using System;
using System.Linq;
using System.Security.Claims;
using CDR.DataHolder.IdentityServer.Configuration;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using CDR.DataHolder.IdentityServer.Extensions;

namespace CDR.DataHolder.IdentityServer.Helpers
{
    public static class ConsentAndAuthenticationHelper
    {
        /// <summary>
        /// The value in the claims get added to the ID Token.
        /// </summary>
        public static void SetClaimsPrincipalAndForceConsentForAuthoriseRequest(
            ValidatedAuthorizeRequest validatedRequest,
            int sharingDurationSeconds,
            string cdrArrangementId,
            IConfiguration configuration,
            IConfigurationSettings configurationSettings,
            IHttpContextAccessor httpContextAccessor)
        {
            validatedRequest.Client.RequireConsent = false; // Needed for CTS to force consent
            var currentIdentity = validatedRequest.Subject?.Identity as ClaimsIdentity;
            if (currentIdentity == null || !currentIdentity.IsAuthenticated)
            {
                return;
            }

            // Remove any existing sharing_expires_at claim.
            var sharingExpiresClaim = currentIdentity.Claims.FirstOrDefault(c => c.Type.Equals(StandardClaims.SharingDurationExpiresAt, StringComparison.OrdinalIgnoreCase));
            if (sharingExpiresClaim != null)
            {
                currentIdentity.RemoveClaim(sharingExpiresClaim);
            }

            // Add the sharing duration for the refresh token
            var expiry = sharingDurationSeconds == 0 ? 0 : DateTimeOffset.UtcNow.AddSeconds(sharingDurationSeconds).ToUnixTimeSeconds();
            currentIdentity.AddClaim(new Claim(StandardClaims.Expiry, expiry.ToString(), ClaimValueTypes.Integer));

            if (configuration.FapiComplianceLevel() <= CdsConstants.FapiComplianceLevel.Fapi1Phase1)
            {
                currentIdentity.AddClaim(new Claim(StandardClaims.SharingDurationExpiresAt, expiry.ToString(), ClaimValueTypes.Integer));
            }

            // Remove any existing cdr_arrangement_id claim.
            var cdrArrangementClaim = currentIdentity.Claims.FirstOrDefault(c => c.Type.Equals(StandardClaims.CDRArrangementId, StringComparison.OrdinalIgnoreCase));
            if (cdrArrangementClaim != null)
            {
                currentIdentity.RemoveClaim(cdrArrangementClaim);
            }

            if (!string.IsNullOrEmpty(cdrArrangementId))
            {
                currentIdentity.AddClaim(new Claim(StandardClaims.CDRArrangementId, cdrArrangementId));
            }
        }
    }
}
