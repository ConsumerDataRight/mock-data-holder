using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Configuration;
using IdentityServer4.Configuration;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class CustomDiscoveryResponseGenerator : DiscoveryResponseGenerator
    {
        private readonly IConfiguration _configuration;

        public CustomDiscoveryResponseGenerator(
            IdentityServerOptions options,
            IResourceStore resourceStore,
            IKeyMaterialService keys,
            ExtensionGrantValidator extensionGrants,
            ISecretsListParser secretParsers,
            IResourceOwnerPasswordValidator resourceOwnerValidator,
            ILogger<CustomDiscoveryResponseGenerator> logger,
            IConfiguration configuration)
            : base(options, resourceStore, keys, extensionGrants, secretParsers, resourceOwnerValidator, logger)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Modifies the Discovery document after initial processing by Ids4.
        /// </summary>
        public override async Task<Dictionary<string, object>> CreateDiscoveryDocumentAsync(string baseUrl, string issuerUri)
        {
            // we want to make sure that the discovery endpoint returns the base uri including the conformanceId.
            Dictionary<string, object> discovery = await base.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

            if (Options.Endpoints.EnableTokenEndpoint)
            {
                var tokenUri = _configuration[Constants.ConfigurationKeys.TokenUri];
                discovery["token_endpoint"] = tokenUri;
            }

            if (Options.Endpoints.EnableTokenRevocationEndpoint)
            {
                var revocationUri = _configuration[Constants.ConfigurationKeys.RevocationUri];
                discovery["revocation_endpoint"] = revocationUri;
            }

			if (discovery.ContainsKey(CdsConstants.Discovery.JwksUriOverride))
			{
				discovery[CdsConstants.Discovery.JwksUri] = discovery[CdsConstants.Discovery.JwksUriOverride];
				discovery.Remove(CdsConstants.Discovery.JwksUriOverride);
			}

			if (discovery.ContainsKey(CdsConstants.Discovery.AuthorizationEndpointOverride))
			{
				discovery[CdsConstants.Discovery.AuthorizationEndpoint] = discovery[CdsConstants.Discovery.AuthorizationEndpointOverride];
				discovery.Remove(CdsConstants.Discovery.AuthorizationEndpointOverride);
			}

			if (discovery.ContainsKey(CdsConstants.Discovery.UserInfoEndpointOverride))
            {
                discovery[CdsConstants.Discovery.UserInfoEndpoint] = discovery[CdsConstants.Discovery.UserInfoEndpointOverride];
                discovery.Remove(CdsConstants.Discovery.UserInfoEndpointOverride);
            }

            // We are overriding this as Ids4 currentl supports only one signing credential,
            // and will never have more than one signing algorithm in this field.
            discovery[CdsConstants.Discovery.IdTokenSigningAlgorithmsSupported] = new[]
            {
                CdsConstants.Algorithms.Signing.PS256,
                CdsConstants.Algorithms.Signing.ES256,
            };

            discovery[CdsConstants.Discovery.AcrValuesSupported] = new[]
            {
                CdsConstants.StandardClaims.ACR2Value,
                CdsConstants.StandardClaims.ACR3Value,
            };

            discovery[CdsConstants.Discovery.ClaimsSupported] = new[]
            {
                "name",
                "given_name",
                "family_name",
                "refresh_token_expires_at",
                "sharing_expires_at",
                "sharing_duration",
                "iss",
                "sub",
                "aud",
                "acr",
                "exp",
                "iat",
                "nonce",
                "auth_time",
                "updated_at"
            };

            discovery[CdsConstants.Discovery.SubjectTypesSupported] = new string[] { CdsConstants.SubjectTypes.Pairwise };

            // Cds does not supporte Pkce.
            discovery.Remove(CdsConstants.Discovery.CodeChallengeMethodsSupported);

            // Scopes supported
            discovery[CdsConstants.Discovery.ScopesSupported] = _configuration["ScopesSupported"].Split(',').ToList();

            // Cds does not support offline_access scope, it is considered built-in the 'sharing_duration' claim,
            // which requires us to replace the fixed size list with a new one.
            var scopesSupported = (IList<string>)discovery[CdsConstants.Discovery.ScopesSupported];
            var updatedScopesSupported = new List<string>(scopesSupported);
            updatedScopesSupported.Remove(CdsConstants.StandardScopes.OfflineAccess);
            discovery[CdsConstants.Discovery.ScopesSupported] = updatedScopesSupported;

            // Rearranging so that the Registration endpoint will appear with the rest of the endpoints,
            // and not part of custom entries.
            return ArrangeDiscoveryDocument(
                discovery,
                CdsConstants.Discovery.RegistrationEndpoint,
                CdsConstants.Discovery.AuthorizationEndpoint);
        }

        private Dictionary<string, object> ArrangeDiscoveryDocument(Dictionary<string, object> discovery, string key1, string key2)
        {
            var discoveryList = discovery.ToList();
            var key1Item = discoveryList.Find(x => x.Key == key1);
            discoveryList.Remove(key1Item);
            var index = discoveryList.FindIndex(x => x.Key == key2);
            discoveryList.Insert(index, key1Item);
            return discoveryList.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}