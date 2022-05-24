using System;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Helpers;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Stores
{
    public class DynamicClientStore : ClientStore
    {
        private readonly IConfiguration _configuration;

        public DynamicClientStore(
            ILogger<ClientStore> logger, 
            IConfiguration configuration,
            ConfigurationDbContext configurationDbContext) :base(logger, configurationDbContext)
        {
            _configuration = configuration;
        }

        public async Task<Client> FindClientBySoftwareProductIdAsync(string softwareProductId)
        {
            _logger.LogInformation($"{nameof(DynamicClientStore)}.{nameof(FindClientBySoftwareProductIdAsync)}");

            var client = await _configurationDbContext.Clients
                .Include(c => c.RedirectUris)
                .Include(c => c.ClientSecrets)
                .Include(c => c.AllowedGrantTypes)
                .Include(c => c.AllowedScopes)
                .Include(c => c.Claims)
                .Where(c => c.Claims.Any(cc => cc.Type == "software_id" && cc.Value == softwareProductId))
                .FirstOrDefaultAsync();

            if (client == null)
            {
                _logger.LogError("Software Product Id {softwareProductId} was not found.", softwareProductId);
                return null;
            }

            return client.ToModel();
        }

        public async Task<bool> RemoveClientByIdAsync(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return false;
            }

            var client = _configurationDbContext.Clients.FirstOrDefault(c => c.ClientId == clientId);
            if (client != null)
            {
                try
                {
                    _configurationDbContext.Clients.Remove(client);
                    var saveResult = await _configurationDbContext.SaveChangesAsync();

                    return saveResult > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Store Client failed for {@Client}", client);
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> StoreClientAsync(Client client)
        {
            var clientEntity = ConvertIdentityServerModelToEntityHelper.ConvertModelClientToEntityClient(client, _configuration);
            clientEntity.ClientSecrets.Add(new IdentityServer4.EntityFramework.Entities.ClientSecret()
            {
                Type = SecretTypes.JsonWebKey,
                Value = clientEntity.Claims.First(c => c.Type == "jwks_uri").Value,
                Description = SecretDescription.Encyption,
            });

            try
            {
                await _configurationDbContext.Clients.AddAsync(clientEntity);
                var saveResult = await _configurationDbContext.SaveChangesAsync();

                return saveResult > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Store Client failed for {@Client}", client);
                throw;
            }
        }
    }
}
