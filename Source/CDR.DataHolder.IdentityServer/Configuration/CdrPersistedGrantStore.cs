using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public class CdrPersistedGrantStore : IPersistedGrantStore
    {
        public Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            return Task.FromResult(Enumerable.Empty<PersistedGrant>());
        }

        public Task<PersistedGrant> GetAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task StoreAsync(PersistedGrant grant)
        {
            return Task.CompletedTask;
        }
    }
}