using System;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class CustomTokenRevocationResponseGenerator : ITokenRevocationResponseGenerator
    {
        protected readonly IHttpContextAccessor HttpContextAccessor;

        /// <summary>
        /// Gets the reference token store.
        /// </summary>
        /// <value>
        /// The reference token store.
        /// </value>
        protected readonly IReferenceTokenStore ReferenceTokenStore;

        /// <summary>
        /// Gets the refresh token store.
        /// </summary>
        /// <value>
        /// The refresh token store.
        /// </value>
        protected readonly IRefreshTokenStore RefreshTokenStore;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTokenRevocationResponseGenerator" /> class.
        /// </summary>
        /// <param name="referenceTokenStore">The reference token store.</param>
        /// <param name="refreshTokenStore">The refresh token store.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor.</param>
        public CustomTokenRevocationResponseGenerator(
            IReferenceTokenStore referenceTokenStore,
            IRefreshTokenStore refreshTokenStore,
            ILogger<CustomTokenRevocationResponseGenerator> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            ReferenceTokenStore = referenceTokenStore;
            RefreshTokenStore = refreshTokenStore;
            Logger = logger;
            HttpContextAccessor = httpContextAccessor;
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
            Logger.LogInformation($"Revoking token {validationResult.Token}...");

            // Attempt to find a matching token in the refresh token store.
            var rt = await RefreshTokenStore.GetRefreshTokenAsync(validationResult.Token);

            // Refresh token was found.
            if (rt != null)
            {
                Logger.LogDebug("Matching refresh_token was found.");

                if (rt.ClientId.Equals(validationResult.Client.ClientId, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogDebug("Refresh token revoked");
                    await RefreshTokenStore.RemoveRefreshTokenAsync(validationResult.Token);

                    // TODO: This is a SHOULD in the standards.
                    //await ReferenceTokenStore.RemoveReferenceTokensAsync(token.SubjectId, token.ClientId);
                }
                else
                {
                    Logger.LogWarning("Client {clientId} denied from revoking a refresh token belonging to Client {tokenClientId}", validationResult.Client.ClientId, rt.ClientId);
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

            await ReferenceTokenStore.RemoveReferenceTokenAsync(validationResult.Token);

            return new TokenRevocationResponse()
            {
                Success = true,
            };
        }
    }
}
