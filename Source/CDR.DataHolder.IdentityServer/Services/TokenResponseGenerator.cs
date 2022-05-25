using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class TokenResponseGenerator : IdentityServer4.ResponseHandling.TokenResponseGenerator
    {
        private readonly ICustomGrantService _customGrantService;
        private readonly IIdPermanenceManager _idPermanenceManager;
        private readonly IConfiguration _configuration;

        public TokenResponseGenerator(
            ISystemClock clock,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            IScopeParser scopeParser,
            IResourceStore resources,
            IClientStore clients,
            IConfiguration configuration,
            ILogger<TokenResponseGenerator> logger,
            IIdPermanenceManager idPermanenceManager,
            ICustomGrantService customGrantService)
        : base(clock, tokenService, refreshTokenService, scopeParser, resources, clients, logger)
        {
            _customGrantService = customGrantService;
            _idPermanenceManager = idPermanenceManager;
            _configuration = configuration;
        }

        protected override async Task<string> CreateIdTokenFromRefreshTokenRequestAsync(ValidatedTokenRequest request, string newAccessToken)
        {
            var resources = await Resources.FindResourcesByScopeAsync(request.RefreshToken.Scopes);
            if (resources.IdentityResources.Any())
            {
                var oldAccessToken = request.RefreshToken.AccessToken;
                var scopes = oldAccessToken.Scopes;

                if (request.RequestedScopes != null && request.RequestedScopes.Any())
                {
                    scopes = request.RequestedScopes;
                }

                var tokenRequest = new TokenCreationRequest
                {
                    Subject = request.RefreshToken.Subject,
                    ValidatedResources = new ResourceValidationResult(await Resources.FindEnabledResourcesByScopeAsync(scopes)),
                    ValidatedRequest = request,
                    AccessTokenToHash = newAccessToken,
                };

                var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
                AddOptionalClaims(idToken, request.RefreshToken != null);

                return await TokenService.CreateSecurityTokenAsync(idToken);
            }

            return null;
        }

        protected override async Task<IdentityServer4.ResponseHandling.TokenResponse> ProcessRefreshTokenRequestAsync(
            TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for refresh token request");

            var refreshToken = request.ValidatedRequest.RefreshTokenHandle;
            var oldAccessToken = request.ValidatedRequest.RefreshToken.AccessToken;
            string accessTokenString;
            var scopes = request.ValidatedRequest.RefreshToken.Scopes;

            if (request.ValidatedRequest.Client.UpdateAccessTokenClaimsOnRefresh)
            {
                var subject = request.ValidatedRequest.RefreshToken.Subject;

                if (request.ValidatedRequest.RequestedScopes != null && request.ValidatedRequest.RequestedScopes.Any())
                {
                    scopes = request.ValidatedRequest.RequestedScopes;
                }

                var creationRequest = new TokenCreationRequest
                {
                    Subject = subject,
                    ValidatedRequest = request.ValidatedRequest,
                    ValidatedResources = new ResourceValidationResult(await Resources.FindEnabledResourcesByScopeAsync(scopes)),
                };

                var newAccessToken = await TokenService.CreateAccessTokenAsync(creationRequest);
                accessTokenString = await TokenService.CreateSecurityTokenAsync(newAccessToken);
            }
            else
            {
                oldAccessToken.CreationTime = Clock.UtcNow.UtcDateTime;
                oldAccessToken.Lifetime = request.ValidatedRequest.AccessTokenLifetime;

                accessTokenString = await TokenService.CreateSecurityTokenAsync(oldAccessToken);
            }

            var response = new IdentityServer4.ResponseHandling.TokenResponse
            {
                IdentityToken = await CreateIdTokenFromRefreshTokenRequestAsync(request.ValidatedRequest, accessTokenString),
                AccessToken = accessTokenString,
                AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
                RefreshToken = refreshToken,
                Custom = request.CustomResponse,
                Scope = scopes.ToSpaceSeparatedString(),
            };

            //////////////////////////
            // cdr arrangement id
            /////////////////////////
            var arrangementId = await CreateOrGetArrangementPersistedGrant(request.ValidatedRequest, refreshToken);
            if (response.Custom == null)
            {
                response.Custom = new Dictionary<string, object>()
                {
                    { StandardClaims.CDRArrangementId, arrangementId },
                };
            }
            else
            {
                response.Custom.Add(StandardClaims.CDRArrangementId, arrangementId);
            }

            return response;
        }

        protected override async Task<IdentityServer4.ResponseHandling.TokenResponse> ProcessAuthorizationCodeRequestAsync(TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for authorization code request");

            var response = new IdentityServer4.ResponseHandling.TokenResponse
            {
                AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
                Custom = request.CustomResponse,
                Scope = request.ValidatedRequest.AuthorizationCode.RequestedScopes.ToSpaceSeparatedString(),
            };

            //////////////////////////
            // Create a skeleton CDR arrangement grant
            /////////////////////////
            var arrangementId = await CreateOrGetArrangementPersistedGrant(request.ValidatedRequest, string.Empty);
            if (response.Custom == null)
            {
                response.Custom = new Dictionary<string, object>()
                {
                    { StandardClaims.CDRArrangementId, arrangementId },
                };
            }
            else
            {
                response.Custom.Add(StandardClaims.CDRArrangementId, arrangementId);
            }

            var claimsPrincipal = request.ValidatedRequest.AuthorizationCode?.Subject;
            if (claimsPrincipal != null)
            {
                claimsPrincipal.Identities.First().AddClaim(
                    new Claim(StandardClaims.CDRArrangementId, arrangementId));
            }

            //////////////////////////
            // access token
            /////////////////////////
            (var accessToken, var refreshToken) = await CreateAccessTokenAsync(request.ValidatedRequest);
            response.AccessToken = accessToken;

            //////////////////////////
            // update the cdr arrangement id with the refresh token
            /////////////////////////
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await CreateOrGetArrangementPersistedGrant(request.ValidatedRequest, refreshToken);
            }

            //////////////////////////
            // refresh token
            /////////////////////////
            if (refreshToken.IsPresent())
            {
                response.RefreshToken = refreshToken;
            }

            //////////////////////////
            // id token
            /////////////////////////
            if (request.ValidatedRequest.AuthorizationCode.IsOpenId)
            {
                // load the client that belongs to the authorization code
                Client client = null;
                if (request.ValidatedRequest.AuthorizationCode.ClientId != null)
                {
                    client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.AuthorizationCode.ClientId);
                }

                if (client == null)
                {
                    throw new InvalidOperationException("Client does not exist anymore.");
                }

                var resources = await Resources.FindEnabledResourcesByScopeAsync(request.ValidatedRequest.AuthorizationCode.RequestedScopes);

                var tokenRequest = new TokenCreationRequest
                {
                    Subject = request.ValidatedRequest.AuthorizationCode.Subject,
                    ValidatedResources = new ResourceValidationResult(resources),
                    Nonce = request.ValidatedRequest.AuthorizationCode.Nonce,
                    AccessTokenToHash = response.AccessToken,
                    StateHash = request.ValidatedRequest.AuthorizationCode.StateHash,
                    ValidatedRequest = request.ValidatedRequest,
                };

                var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);

                AddOptionalClaims(idToken, refreshToken.IsPresent());

                var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
                response.IdentityToken = jwt;
            }

            return response;
        }

        /// <summary>
        /// Creates the access/refresh token.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Tuple of accessToken and refreshToken.</returns>
        /// <exception cref="System.InvalidOperationException">Client does not exist anymore.</exception>
        protected override async Task<(string accessToken, string refreshToken)> CreateAccessTokenAsync(ValidatedTokenRequest request)
        {
            TokenCreationRequest tokenRequest;
            bool createRefreshToken = false;

            if (request.AuthorizationCode != null)
            {
                if (int.TryParse(request.AuthorizationCode.Subject.Claims.FirstOrDefault(x => x.Type == StandardClaims.SharingDurationExpiresAt)?.Value, out int sharingExpiresAt))
                {
                    createRefreshToken = sharingExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }

                // load the client that belongs to the authorization code
                Client client = null;
                if (request.AuthorizationCode.ClientId != null)
                {
                    client = await Clients.FindEnabledClientByIdAsync(request.AuthorizationCode.ClientId);
                }

                if (client == null)
                {
                    throw new InvalidOperationException("Client does not exist anymore.");
                }

                var resources = await Resources.FindEnabledResourcesByScopeAsync(request.AuthorizationCode.RequestedScopes);

                tokenRequest = new TokenCreationRequest
                {
                    Subject = request.AuthorizationCode.Subject,
                    ValidatedResources = new ResourceValidationResult(resources),
                    ValidatedRequest = request,
                };
            }
            else if (request.DeviceCode != null)
            {
                throw new InvalidOperationException("DeviceCode not supported.");
            }
            else
            {
                createRefreshToken = request.ValidatedResources.RawScopeValues.Contains(StandardScopes.OfflineAccess);

                tokenRequest = new TokenCreationRequest
                {
                    Subject = request.Subject,
                    ValidatedResources = request.ValidatedResources,
                    ValidatedRequest = request,
                };
            }

            var at = await TokenService.CreateAccessTokenAsync(tokenRequest);
            var accessToken = await TokenService.CreateSecurityTokenAsync(at);

            // If updating an existing CDR Arrangement, delete any existing refresh token.
            var cdrArrangementId = at.Claims.GetClaimValue<string>(CdsConstants.StandardClaims.CDRArrangementId);
            if (!string.IsNullOrEmpty(cdrArrangementId))
            {
                var searchTerm = $"\"Type\":\"cdr_arrangement_id\",\"Value\":\"{cdrArrangementId}\"";
                var grant = await _customGrantService.GetGrantByKeyword(
                    at.Claims.GetClaimValue<string>(CdsConstants.StandardClaims.Sub),
                    CdsConstants.GrantTypes.RefreshToken,
                    searchTerm);

                if (grant != null)
                {
                    await _customGrantService.RemoveGrant(grant.Key);
                }
            }

            if (createRefreshToken)
            {
                var refreshToken = await RefreshTokenService.CreateRefreshTokenAsync(tokenRequest.Subject, at, request.Client);

                return (accessToken, refreshToken);
            }

            return (accessToken, null);
        }

        private async Task<string> CreateOrGetArrangementPersistedGrant(
            ValidatedTokenRequest validatedRequest,
            string refreshToken)
        {
            // Decrypt the subject id.
            var sub = GetSubjectId(validatedRequest);
            var cdrArrangementId = validatedRequest.Subject.GetClaimValue(CdsConstants.StandardClaims.CDRArrangementId);
            var existingGrant = await GetExistingCdrArrangementGrant(sub, cdrArrangementId, validatedRequest.AuthorizationCodeHandle, refreshToken);

            var cdrArrangementGrant = new CdrArrangementGrant
            {
                RefreshTokenKey = string.IsNullOrEmpty(refreshToken) ? string.Empty : $"{refreshToken}:{CdsConstants.GrantTypes.RefreshToken}".Sha256(),
                Subject = sub,
            };
            var cdrArrangementGrantJson = JsonConvert.SerializeObject(cdrArrangementGrant, Formatting.None);

            if (existingGrant != null)
            {
                existingGrant.Data = cdrArrangementGrantJson;
                return await _customGrantService.StoreGrant(existingGrant);
            }

            // generate a new cdr arrangement id
            var persistedGrant = new PersistedGrant()
            {
                Key = Guid.NewGuid().ToString(),
                ClientId = validatedRequest.ClientId,
                SubjectId = sub,
                Data = cdrArrangementGrantJson,
                Type = CdsConstants.GrantTypes.CdrArrangementGrant,
                CreationTime = DateTime.UtcNow,
            };

            // create a new cdrArrangementId since it does not exist in db
            return await _customGrantService.StoreGrant(persistedGrant);
        }

        private string GetSubjectId(ValidatedTokenRequest validatedRequest)
        {
            // If the sub claim value is a guid, then no need to decrypt.
            var sub = validatedRequest.Subject.GetSubjectId();
            if (Guid.TryParse(sub, out _))
            {
                return sub;
            }

            // Extract the claim values needed to decrypt the sub claim.
            var softwareId = validatedRequest.Subject.Claims.GetClaimValue("software_id");
            var sectorIdentifierUri = validatedRequest.Subject.Claims.GetClaimValue("sector_identifier_uri");

            if (string.IsNullOrEmpty(softwareId) || string.IsNullOrEmpty(sectorIdentifierUri))
            {
                return sub;
            }

            // Decrypt the sub claim via the id permanence algorithm.
            var param = new SubPermanenceParameters()
            {
                SoftwareProductId = softwareId,
                SectorIdentifierUri = sectorIdentifierUri
            };
            return _idPermanenceManager.DecryptSub(sub, param);
        }

        private async Task<PersistedGrant> GetExistingCdrArrangementGrant(string sub, string cdrArrangementId, string authorizationCode, string refreshToken)
        {
            // Find the grant by cdr arrangement id, subject id and grant type.
            var grant = await _customGrantService.GetGrant(cdrArrangementId, sub, CdsConstants.GrantTypes.CdrArrangementGrant);
            if (grant != null)
            {
                return grant;
            }

            // Not found by cdr arrangement id, so try and find by authorization code or refresh token.
            var keywordInData = authorizationCode != null ? authorizationCode : $"{refreshToken}:{CdsConstants.GrantTypes.RefreshToken}".Sha256();
            return await _customGrantService.GetGrantByKeyword(sub, CdsConstants.GrantTypes.CdrArrangementGrant, keywordInData);
        }

        private void AddOptionalClaims(Token idToken, bool refreshTokenCreated)
        {
            if (_configuration.FapiComplianceLevel() <= FapiComplianceLevel.Fapi1Phase1)
            {
                _ = int.TryParse(idToken.Claims.FirstOrDefault(x => x.Type == StandardClaims.SharingDurationExpiresAt)?.Value, out int sharingExpiresAt);
                int refreshTokenExpiresAt = refreshTokenCreated ? sharingExpiresAt : 0;
                idToken.Claims.Add(new Claim(StandardClaims.RefreshTokenExpiresAt, refreshTokenExpiresAt.ToString(), ClaimValueTypes.Integer));
            }
        }
    }
}