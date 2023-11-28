using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using Microsoft.Extensions.Configuration;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType, string? defaultValue = null)
        {
            return claimsPrincipal.Claims.GetClaimValue(claimType, defaultValue);
        }

        public static T? GetClaimValue<T>(this ClaimsPrincipal claimsPrincipal, string claimType, T? defaultValue = default)
        {
            return claimsPrincipal.Claims.GetClaimValue(claimType, defaultValue);
        }

        public static string? GetClaimValue(this IEnumerable<Claim> claims, string claimType, string? defaultValue = null)
        {
            var claim = claims.FirstOrDefault(c => c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase));
            if (claim != null)
            {
                return claim.Value;
            }

            return defaultValue;
        }

        public static T? GetClaimValue<T>(this IEnumerable<Claim> claims, string claimType, T? defaultValue = default)
        {
            var claimValueString = GetClaimValue(claims, claimType, defaultValue as string);
            return (T?)Convert.ChangeType(claimValueString, typeof(T?));
        }

        public static string GetSubject(this ClaimsPrincipal claimsPrincipal, IConfiguration config)
        {
            string? sub = claimsPrincipal.GetClaimValue("sub");

            var param = new SubPermanenceParameters()
            {
                SoftwareProductId = claimsPrincipal.GetClaimValue("software_id"),
                SectorIdentifierUri = claimsPrincipal.GetClaimValue("sector_identifier_uri")
            };

            return IdPermanenceHelper.DecryptSub(sub, param, IdPermanenceHelper.GetPrivateKey(config));
        }

        public static string? GetSoftwareProductId(this ClaimsPrincipal claimsPrincipal)
        {            
            return claimsPrincipal.GetClaimValue("software_id");                        
        }

        public static string GetCustomerLoginId(this ClaimsPrincipal principal)
        {
            var loginId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(loginId))
            {
                return string.Empty;
            }
            return loginId;
        }

        public static string[] GetAccountIds(this ClaimsPrincipal principal)
        {
            // Check if consumer has granted consent to this account Id
            return principal.FindAll(Constants.TokenClaimTypes.AccountId)
                .Select(c => c.Value)
                .ToArray();
        }
    }
}
