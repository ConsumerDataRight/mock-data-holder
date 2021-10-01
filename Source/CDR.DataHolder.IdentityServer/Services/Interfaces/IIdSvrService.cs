using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;

namespace CDR.DataHolder.IdentityServer.Services.Interfaces
{
    public interface IIdSvrService
    {
        Task<PersistedGrant> GetCdrArrangementGrantAsync(string clientId, string sub);
    }
}
