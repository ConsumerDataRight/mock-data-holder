using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class CustomReferenceTokenStore : IReferenceTokenStore
    {
        protected readonly IMemoryCache _memCache;

        public CustomReferenceTokenStore(IMemoryCache memCache)
        {
            _memCache = memCache;
        }

        public async Task<Token> GetReferenceTokenAsync(string handle)
        {
            if (_memCache.TryGetValue<bool>(handle, out _))
            {
                return null;
            }

            return new Token(TokenTypes.AccessToken);
        }

        public async Task RemoveReferenceTokenAsync(string handle)
        {
            _memCache.Set(handle, true);
        }

        public async Task RemoveReferenceTokensAsync(string subjectId, string clientId)
        {
            // Not implemented.
        }

        public async Task<string> StoreReferenceTokenAsync(Token token)
        {
            // Not implemented.
            return null;
        }
    }
}
