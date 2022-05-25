using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IRevokedTokenStore
    {
        Task<bool> IsRevoked(string token);
        Task Add(string token);
    }
}
