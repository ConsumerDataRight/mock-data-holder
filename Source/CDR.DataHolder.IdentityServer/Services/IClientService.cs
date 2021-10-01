using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Services
{
    public interface IClientService
    {
        Task<Client> FindClientById(string clientId);

        Task<Client> FindClientBySoftwareProductId(string softwareProductId);

        Task<bool> RemoveClientById(string clientId);

        Task<bool> RegisterClient(DataReceipientClient client);

        Task<bool> UpdateClient(DataReceipientClient client);
    }
}
