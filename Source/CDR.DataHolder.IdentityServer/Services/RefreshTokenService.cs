using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class RefreshTokenService : DefaultRefreshTokenService
    {
        private readonly ILogger _logger;

        public RefreshTokenService(IRefreshTokenStore refreshTokenStore, IProfileService profileService, ISystemClock clock, ILogger<DefaultRefreshTokenService> logger)
            : base(refreshTokenStore, profileService, clock, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates the refresh token.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// The refresh token handle.
        /// </returns>
        public override async Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, Token accessToken, Client client)
        {
            _logger.LogDebug("Creating refresh token");

            var now = Clock.UtcNow;
            var epochNow = now.ToUnixTimeSeconds();
            _ = int.TryParse(subject.Claims.FirstOrDefault(c => c.Type == StandardClaims.SharingDurationExpiresAt)?.Value, out int sharingExpiresAt);

            long lifetime = sharingExpiresAt > epochNow ? sharingExpiresAt - epochNow : 0;

            var refreshToken = new RefreshToken
            {
                CreationTime = now.DateTime,
                Lifetime = (int)lifetime,
                AccessToken = accessToken,
            };

            return await RefreshTokenStore.StoreRefreshTokenAsync(refreshToken);
        }
    }
}