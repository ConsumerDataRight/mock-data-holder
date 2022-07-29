using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Models;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Services
{
    public interface IClientService
    {
        Task<Client> FindClientById(string clientId);

        Task<Client> FindClientBySoftwareProductId(string softwareProductId);

        Task<bool> RemoveClientById(string clientId);

        Task<bool> RegisterClient(DataRecipientClient client);

        Task<bool> UpdateClient(DataRecipientClient client);

        Task<Client> RefreshJwks(string clientId);

        Task EnsureKid(string clientId, string jwt, TokenValidationParameters tokenValidationParameters);
    }
}
