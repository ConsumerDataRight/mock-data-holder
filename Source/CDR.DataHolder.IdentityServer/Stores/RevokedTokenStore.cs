using CDR.DataHolder.IdentityServer.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Stores
{
    public class RevokedTokenStore : IRevokedTokenStore
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RevokedTokenStore> _logger;

        public RevokedTokenStore(
            IDistributedCache cache,
            ILogger<RevokedTokenStore> logger) 
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task Add(string token)
        {
            _logger.LogDebug("Adding token to revoked token store: {token}", token);
            try
            {
                await _cache.SetStringAsync(GetKey(token), bool.TrueString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while adding token to cache");
                throw;
            }
        }

        public async Task<bool> IsRevoked(string token)
        {
            var isRevoked = await _cache.GetStringAsync(GetKey(token));
            _logger.LogDebug("Token {token} is revoked = {isRevoked}", token, isRevoked);
            return !string.IsNullOrEmpty(isRevoked) && isRevoked.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        private string GetKey(string token)
        {
            return $"RevokedToken:{token}";
        }
    }
}
