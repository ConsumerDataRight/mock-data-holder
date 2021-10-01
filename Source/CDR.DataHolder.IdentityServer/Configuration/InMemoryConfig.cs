using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public static class InMemoryConfig
    {
        public static IEnumerable<ApiResource> Apis =>
            new List<ApiResource>
            {
                new ApiResource("cds-au", "Mock Data Holder (MDH) Resource API")
                {
                    // TODO: Need to add all the scopes supported by cds-au api.
                    // All the scopes supported by the cds-au API.
					Scopes = new string[] { "cdr:registration", "bank:accounts.detail:read", "bank:accounts.basic:read", "bank:transactions:read", "common:customer.basic:read" }
                }
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new[]
            {
                // These are the supported scopes for this data holder implementation.
                new ApiScope("cdr:registration", "Dynamic Client Registration (DCR)"),
                new ApiScope("bank:accounts.basic:read", "Basic read access to bank accounts"),
                new ApiScope("bank:transactions:read", "Read access to bank account transactions"),
                new ApiScope("common:customer.basic:read", "Basic read access to customer information"),

                // These are the additional scopes for CDR.  These need to be here to allow a DR with more scopes than supported to authorise.
                new ApiScope("bank:accounts.detail:read", "Detailed read access to bank accounts"),
                new ApiScope("bank:payees:read", "This scope allows access to payee information stored by the customer."),
                new ApiScope("bank:regular_payments:read", "The scope would allow the third party to access regular payments. Includes Direct Debits and Scheduled Payments."),
                new ApiScope("common:customer.detail:read", "The scope would allow the third party to access more detailed information about the customer. Includes the data available with the Basic Customer Data scope plus contact details."),
                new ApiScope("admin:metrics.basic:read", "Metrics data accessible ONLY to the CDR Register"),
                new ApiScope("admin:metadata:update", "Update notification accessible ONLY to the CDR Register"),
            };

        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };
    }
}