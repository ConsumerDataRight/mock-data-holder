using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class ClaimExtensions
    {
        public static string Get(this ICollection<ClientClaim> claims, string name)
        {
            var claim = claims.FirstOrDefault(c => c.Type.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (claim == null)
            {
                return string.Empty;
            }

            return claim.Value;
        }
    }
}
