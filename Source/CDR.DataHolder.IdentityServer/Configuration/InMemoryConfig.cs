using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public static class InMemoryConfig
    {
        public static IEnumerable<ApiResource> Apis(IConfiguration config) =>
            new List<ApiResource>
            {
                new ApiResource("cds-au", "Mock Data Holder (MDH) Resource API")
                {
                    Scopes = config["ScopesSupported"].Split(',')
                }
            };

        public static IEnumerable<ApiScope> ApiScopes(IConfiguration config)
        {
            var apiScopes = new List<ApiScope>();

            // These are the supported scopes for this data holder implementation.
            foreach (var scope in config["ScopesSupported"].Split(','))
            {
                if (scope != API.Infrastructure.Constants.StandardScopes.OpenId && scope != API.Infrastructure.Constants.StandardScopes.Profile)
                {
                    apiScopes.Add(new ApiScope(scope));
                }
            }

            // These are the additional scopes for CDR.  These need to be here to allow a DR with more scopes than supported to authorise.
            apiScopes.Add(new ApiScope(API.Infrastructure.Constants.CdrScopes.MetricsBasicRead, "Metrics data accessible ONLY to the CDR Register"));
            apiScopes.Add(new ApiScope(API.Infrastructure.Constants.CdrScopes.MetadataUpdate, "Update notification accessible ONLY to the CDR Register"));

            return apiScopes;
        }

        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };
    }
}