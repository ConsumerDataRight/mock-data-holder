using System;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class CustomJwtRequestUriHttpClient : IJwtRequestUriHttpClient
    {
        private readonly ICustomGrantService _customGrantService;
        private readonly ILogger _logger;

        public CustomJwtRequestUriHttpClient(
            ILogger<CustomJwtRequestUriHttpClient> logger,
            ICustomGrantService customGrantService)
        {
            _logger = logger;
            _customGrantService = customGrantService;
        }

        public async Task<string> GetJwtAsync(string url, Client client)
        {
            // Retrieve the persisted grant by key.
            var key = url.Replace(AuthorizeRequest.RequestUriPrefix, "");
            var grant = await _customGrantService.GetGrant(key);
            if (grant == null)
            {
                _logger.LogError("{invalidRequestUri}: Request URI could not be found.", AuthorizeErrorCodes.InvalidRequestUri);
                return null;
            }

            // Check that the grant was for the intended client.
            if (!grant.ClientId.Equals(client.ClientId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("{invalidRequestUri}: Request URI could not be found for the client.", AuthorizeErrorCodes.InvalidRequestUri);
                return null;
            }

            // Check that the grant has not expired.
            if (!grant.Expiration.HasValue || grant.Expiration < DateTime.UtcNow)
            {
                _logger.LogError("{invalidRequestUri}: Request URI is expired.", AuthorizeErrorCodes.InvalidRequestUri);
                return null;
            }

            // The request jwt is found in the Data field of the persisted grant.
            return grant.Data;
        }
    }
}
