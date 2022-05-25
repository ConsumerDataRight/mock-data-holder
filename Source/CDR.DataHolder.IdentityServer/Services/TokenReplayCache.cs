using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class TokenReplayCache : ITokenReplayCache
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<ITokenReplayCache> _logger;

        public TokenReplayCache(IDistributedCache cache, ILogger<ITokenReplayCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool TryAdd(string securityToken, DateTime expiresOn)
        {
            try
            {
                _cache.SetString(GetKey(securityToken), securityToken, new DistributedCacheEntryOptions() { AbsoluteExpiration = new DateTimeOffset(expiresOn) });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while adding security token to cache");
                throw;
            }
        }

        public bool TryFind(string securityToken)
        {
            return !string.IsNullOrEmpty(_cache.GetString(GetKey(securityToken)));
        }

        private string GetKey(string token)
        {
            return $"TokenReplay:{token}";
        }
    }
}