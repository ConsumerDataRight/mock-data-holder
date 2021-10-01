using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class IdSvrService : IIdSvrService
    {
        private readonly PersistedGrantDbContext _persistedGrantDbContext;

        public IdSvrService(PersistedGrantDbContext persistedGrantDbContext)
        {
            _persistedGrantDbContext = persistedGrantDbContext;
        }

        public async Task<PersistedGrant> GetCdrArrangementGrantAsync(string clientId, string sub)
        {
            var grant = await _persistedGrantDbContext.PersistedGrants
                                    .Where(g => g.Type == "cdr_arrangement_grant" && g.SubjectId == sub && g.ClientId == clientId)
                                    .OrderByDescending(x => x.CreationTime).FirstAsync();

            return grant;
        }
    }
}
