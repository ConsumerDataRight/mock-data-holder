using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface ICustomGrantService
    {
        Task<PersistedGrant> GetGrant(string keyId);

        Task<PersistedGrant> GetGrant(string keyId, string subjectId, string grantType);

        Task<PersistedGrant> GetGrantByKeyword(string subjectId, string grantType, string keywordInData);

        Task<string> StoreGrant(PersistedGrant persistedGrant);

        Task RemoveGrant(string key);

        Task<int> RemoveGrantsForCdrArrangementId(string cdrArrangementId, string clientId);

        Task<bool> UpdateCdrArrangementGrant(string cdrArrangementId, string authCode);

        Task<string> GetAlternativeCdrArrangementIdFromSubjectGrants(string cdrArrangementId, string clientId);
    }
}
