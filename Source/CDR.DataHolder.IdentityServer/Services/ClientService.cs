using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System.Linq;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Stores;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class ClientService : IClientService
    {
        private readonly ILogger<ClientService> _logger;
        private readonly DynamicClientStore _clientStore;
        private readonly IConfigurationSettings _configurationSettings;

        public ClientService(
            ILogger<ClientService> logger,
            DynamicClientStore clientStore, 
            IConfigurationSettings configurationSettings)
        {
            _logger = logger;
            _configurationSettings = configurationSettings;
            _clientStore = clientStore;
        }

        public Task<Client> FindClientById(string clientId)
            => _clientStore.FindClientByIdAsync(clientId);

        public Task<Client> FindClientBySoftwareProductId(string softwareProductId)
             => _clientStore.FindClientBySoftwareProductIdAsync(softwareProductId);

        public Task<bool> RemoveClientById(string clientId)
            => _clientStore.RemoveClientByIdAsync(clientId);

        public async Task<bool> RegisterClient(DataRecipientClient client)
        {
            client.AccessTokenLifetime = _configurationSettings.AccessTokenLifetimeSeconds;
            return await _clientStore.StoreClientAsync(client);
        }

        public async Task<bool> UpdateClient(DataRecipientClient client)
        {
            client.AccessTokenLifetime = _configurationSettings.AccessTokenLifetimeSeconds;

            await _clientStore.RemoveClientByIdAsync(client.ClientId);

            return await _clientStore.StoreClientAsync(client);
        }

        public async Task<Client> RefreshJwks(string clientId)
            => await _clientStore.RefreshJwks(clientId);

        public async Task EnsureKid(string clientId, string jwt, TokenValidationParameters tokenValidationParameters)
        {
            // Read the token without validation, to retrieve the kid from the header.
            var handler = new JwtSecurityTokenHandler();
            var unvalidatedToken = handler.ReadJwtToken(jwt);

            // Check if the incoming jwt has a key id that is including in the client's secret keys.
            if (unvalidatedToken.Header.Kid != null
                && !tokenValidationParameters.IssuerSigningKeys.Any(k => k.KeyId.Equals(unvalidatedToken.Header.Kid, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Matching kid ({kid}) not found in client secrets, refreshing jwks from client...", unvalidatedToken.Header.Kid);

                // Is there a matching key id between the clientAssertion JWT and the JWKS stored for the client?
                // If not, reload the JWKS from the client.
                var refreshedClient = await RefreshJwks(clientId);
                tokenValidationParameters.IssuerSigningKeys = await refreshedClient.ClientSecrets.GetKeysAsync();
            }
        }
    }
}