using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Jwk;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Stores
{
    public class ClientStore : IClientStore
    {
        protected readonly ILogger<ClientStore> _logger;
        protected readonly ConfigurationDbContext _configurationDbContext;
        protected readonly IDistributedCache _cache;

        public ClientStore(
            ILogger<ClientStore> logger,
            ConfigurationDbContext configurationDbContext,
            IDistributedCache cache)
        {
            _logger = logger;
            _configurationDbContext = configurationDbContext;
            _cache = cache;
        }

        public async Task<IdentityServer4.Models.Client> FindClientByIdAsync(string clientId)
        {
            _logger.LogInformation($"{nameof(ClientStore)}.{nameof(FindClientByIdAsync)}");

            var client = await _configurationDbContext.Clients.AsNoTracking()
                .Include(c => c.RedirectUris)
                .Include(c => c.ClientSecrets)
                .Include(c => c.AllowedGrantTypes)
                .Include(c => c.AllowedScopes)
                .Include(c => c.Claims)
                .FirstOrDefaultAsync(x => x.ClientId == clientId);

            if (client == null)
            {
                _logger.LogError("Client {ClientId} is not found.", clientId);
                return null;
            }

            return await GetClientSecrets(client, true);
        }

        public async Task<IdentityServer4.Models.Client> RefreshJwks(string clientId)
        {
            _logger.LogInformation($"{nameof(ClientStore)}.{nameof(RefreshJwks)}");

            var client = await _configurationDbContext.Clients.AsNoTracking()
                .Include(c => c.RedirectUris)
                .Include(c => c.ClientSecrets)
                .Include(c => c.AllowedGrantTypes)
                .Include(c => c.AllowedScopes)
                .Include(c => c.Claims)
                .FirstOrDefaultAsync(x => x.ClientId == clientId);

            if (client == null)
            {
                _logger.LogError("Client {ClientId} is not found.", clientId);
                return null;
            }

            return await GetClientSecrets(client, false);
        }

        private async Task<IdentityServer4.Models.Client> GetClientSecrets(Client client, bool checkCache = true)
        {
            _logger.LogInformation($"{nameof(ClientStore)}.{nameof(GetClientSecrets)}");
            _logger.LogInformation("Client secrets: {secretCount}", client.ClientSecrets.Count);

            var updatedClientSecrets = new List<ClientSecret>();
            foreach (var clientSecret in client.ClientSecrets)
            {
                _logger.LogDebug("Client ID: {clientId}. Client secret type: {type}. Client secret value: {value}. Is url: {isUrl}", client.ClientId, clientSecret.Type, clientSecret.Value, Uri.IsWellFormedUriString(clientSecret.Value, UriKind.Absolute));

                if (clientSecret.Type == SecretTypes.JsonWebKey && Uri.IsWellFormedUriString(clientSecret.Value, UriKind.Absolute))
                {
                    try
                    {
                        _logger.LogInformation("Retrieving JWKS from {secretCount}.  Check cache: {checkCache}", clientSecret.Value, checkCache);

                        var jwks = await GetJwks(clientSecret.Value, checkCache);
                        updatedClientSecrets.AddRange(
                            jwks.Keys.Select(key => ConvertJwkToClientSecret(key)));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get JWKs from JwksUri endpoint {jwksUri}", clientSecret.Value);

                        // Failed to retrieve the client secret jwks, so just re-use the old one.
                        updatedClientSecrets.Add(clientSecret);
                    }
                }
                else
                {
                    _logger.LogInformation("A non JWK secret found: {type}", clientSecret.Type);
                    updatedClientSecrets.Add(clientSecret);
                }
            }

            client.ClientSecrets = updatedClientSecrets;

            return client.ToModel();
        }

        private static ClientSecret ConvertJwkToClientSecret(IdentityModel.Jwk.JsonWebKey jsonWebKey)
        {
            return new ClientSecret
            {
                Type = SecretTypes.JsonWebKey,
                Value = JsonConvert.SerializeObject(jsonWebKey),
                Description = SecretDescription.Encyption
            };
        }

        /// <summary>
        /// Note:
        /// This can be cached for a short period of time for performance enhancement
        /// </summary>
        private async Task<JsonWebKeySet> GetJwks(string jwksEndpoint, bool checkCache = true)
        {
            _logger.LogInformation($"{nameof(ClientStore)}.{nameof(GetJwks)}");

            if (checkCache)
            {
                // Checking jwks is in cache.
                var item = await _cache.GetStringAsync(jwksEndpoint);
                if (!string.IsNullOrEmpty(item))
                {
                    _logger.LogInformation("Cache hit: {jwksUri}", jwksEndpoint);
                    _logger.LogDebug("Cache hit contents: {item}", item);
                    return new JsonWebKeySet(item);
                }
            }

            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            _logger.LogInformation("Retrieving JWKS from: {jwksEndpoint}", jwksEndpoint);

            var jwksClient = new HttpClient(clientHandler);
            var jwksResponse = await jwksClient.GetAsync(jwksEndpoint);
            var jwks = await jwksResponse.Content.ReadAsStringAsync();

            _logger.LogDebug("JWKS: {jwks}", jwks);

            _logger.LogDebug("Adding {jwksUri} to cache...", jwksEndpoint);
            _cache.SetString(jwksEndpoint, jwks);

            return new JsonWebKeySet(jwks);
        }
    }
}
