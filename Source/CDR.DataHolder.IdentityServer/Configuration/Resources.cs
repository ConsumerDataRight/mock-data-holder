using IdentityServer4.Models;
using System.Collections.Generic;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public static class Resources
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource> {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };
        }
    }
}
