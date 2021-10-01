using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class TokenReplayCache : ITokenReplayCache
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ITokenReplayCache> _logger;

        public TokenReplayCache(IMemoryCache cache, ILogger<ITokenReplayCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool TryAdd(string securityToken, DateTime expiresOn)
        {
            try
            {
                _cache.Set(securityToken, securityToken, new DateTimeOffset(expiresOn));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while adding security token to in memory cache");
                throw;
            }
        }

        public bool TryFind(string securityToken)
        {
            return _cache.TryGetValue(securityToken, out string _);
        }
    }
}