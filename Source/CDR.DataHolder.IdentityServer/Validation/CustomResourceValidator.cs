using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class CustomResourceValidator : DefaultResourceValidator
    {
        public CustomResourceValidator(
            IResourceStore store,
            IScopeParser scopeParser,
            ILogger<DefaultResourceValidator> logger) : base(store, scopeParser, logger)
        {
        }

        /// <summary>
        /// Override this method to ignore any unsupported scopes.
        /// </summary>
        protected override async Task ValidateScopeAsync(Client client, Resources resourcesFromStore, ParsedScopeValue requestedScope, ResourceValidationResult result)
        {
            var scope = requestedScope.ParsedName;

            if (scope == IdentityServerConstants.StandardScopes.OfflineAccess)
            {
                result.Resources.OfflineAccess = true;
                result.ParsedScopes.Add(new ParsedScopeValue(IdentityServerConstants.StandardScopes.OfflineAccess));
                return;
            }

            var identity = resourcesFromStore.FindIdentityResourcesByScope(scope);
            if (identity != null)
            {
                if (await IsClientAllowedIdentityResourceAsync(client, identity))
                {
                    result.ParsedScopes.Add(requestedScope);
                    result.Resources.IdentityResources.Add(identity);
                    return;
                }
                else
                {
                    result.InvalidScopes.Add(scope);
                }
            }

            // Does client have access to the scope?
            if (client.AllowedScopes.Contains(scope))
            {
                var apis = resourcesFromStore.FindApiResourcesByScope(scope);
                foreach (var api in apis)
                {
                    result.Resources.ApiResources.Add(api);
                    result.ParsedScopes.Add(requestedScope);
                }
            }
        }
    }
}
