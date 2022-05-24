using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class CustomTokenRevocationResponseGenerator : ITokenRevocationResponseGenerator
    {
        private readonly IRevokedTokenStore _revokedTokenStore;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTokenRevocationResponseGenerator" /> class.
        /// </summary>
        /// <param name="revokedTokenStore">The revoked token store.</param>
        /// <param name="refreshTokenStore">The refresh token store.</param>
        /// <param name="logger">The logger.</param>
        public CustomTokenRevocationResponseGenerator(
            IRevokedTokenStore revokedTokenStore,
            IRefreshTokenStore refreshTokenStore,
            ILogger<CustomTokenRevocationResponseGenerator> logger)
        {
            _revokedTokenStore = revokedTokenStore;
            _refreshTokenStore = refreshTokenStore;
            _logger = logger;
        }

        /// <summary>
        /// Creates the revocation endpoint response and processes the revocation request.
        /// </summary>
        /// <param name="validationResult">The userinfo request validation result.</param>
        /// <returns></returns>
        public virtual async Task<TokenRevocationResponse> ProcessAsync(TokenRevocationRequestValidationResult validationResult)
        {
            return await RevokeToken(validationResult);
        }

        /// <summary>
        /// This method will revoke an access token or a refresh token.
        /// </summary>
        protected virtual async Task<TokenRevocationResponse> RevokeToken(TokenRevocationRequestValidationResult validationResult)
        {
            _logger.LogInformation("Revoking token {token}...", validationResult.Token);

            // Attempt to find a matching token in the refresh token store.
            var rt = await _refreshTokenStore.GetRefreshTokenAsync(validationResult.Token);

            // Refresh token was found.
            if (rt != null)
            {
                _logger.LogDebug("Matching refresh_token was found.");

                if (rt.ClientId.Equals(validationResult.Client.ClientId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Refresh token revoked");
                    await _refreshTokenStore.RemoveRefreshTokenAsync(validationResult.Token);
                }
                else
                {
                    _logger.LogWarning("Client {clientId} denied from revoking a refresh token belonging to Client {tokenClientId}", validationResult.Client.ClientId, rt.ClientId);
                }

                return new TokenRevocationResponse()
                {
                    Success = true,
                    TokenType = CdsConstants.TokenTypes.RefreshToken
                };
            }

            //
            // Refresh token not found, so we will now mark the access token as "revoked".
            //
            // Note:
            // -----
            // We are not using reference tokens in this solution as we want to continue to use JWT access tokens
            // as they are easier to inspect the contents for educational purposes.
            // In a production system we would use reference tokens in order to support revocation.
            //

            try
            {
                // Only revoke the access token if the current client owns the access token.
                var securityToken = new JwtSecurityTokenHandler().ReadJwtToken(validationResult.Token);
                if (securityToken != null)
                {
                    var clientIdFromAccessToken = securityToken.Claims.GetClaimValue("client_id");
                    var clientIdFromRequest = validationResult.Client.ClientId;

                    _logger.LogDebug("Incoming client id: {clientId}", clientIdFromRequest);
                    _logger.LogDebug("Access token client id: {clientId}", clientIdFromAccessToken);

                    if (clientIdFromRequest.Equals(clientIdFromAccessToken))
                    {
                        _logger.LogDebug("Revoking access token: {token}", validationResult.Token);
                        await _revokedTokenStore.Add(validationResult.Token);

                        return new TokenRevocationResponse()
                        {
                            Success = true,
                            TokenType = CdsConstants.TokenTypes.AccessToken
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred reading the access token: {token}", validationResult.Token);
            }

            return new TokenRevocationResponse()
            {
                Success = true,
            };
        }
    }
}
