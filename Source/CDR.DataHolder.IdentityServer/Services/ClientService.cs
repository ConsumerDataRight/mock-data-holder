using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Stores;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class ClientService : IClientService
    {
        private readonly DynamicClientStore _clientStore;
        private readonly IConfigurationSettings _configurationSettings;

        public ClientService(DynamicClientStore clientStore, IConfigurationSettings configurationSettings)
        {
            _configurationSettings = configurationSettings;
            _clientStore = clientStore;
        }

        public Task<Client> FindClientById(string clientId)
            => _clientStore.FindClientByIdAsync(clientId);

        public Task<Client> FindClientBySoftwareProductId(string softwareProductId)
             => _clientStore.FindClientBySoftwareProductIdAsync(softwareProductId);

        public Task<bool> RemoveClientById(string clientId)
            => _clientStore.RemoveClientByIdAsync(clientId);

        public async Task<bool> RegisterClient(DataReceipientClient client)
        {
            client.AccessTokenLifetime = _configurationSettings.AccessTokenLifetimeSeconds;
            return await _clientStore.StoreClientAsync(client);
        }

        public async Task<bool> UpdateClient(DataReceipientClient client)
        {
            client.AccessTokenLifetime = _configurationSettings.AccessTokenLifetimeSeconds;

            await _clientStore.RemoveClientByIdAsync(client.ClientId);

            return await _clientStore.StoreClientAsync(client);
        }
    }
}