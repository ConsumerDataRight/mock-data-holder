using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.IdentityServer.Extensions;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Services
{
    // Copied from Identity Server 4 DefaultTokenService, modified for updating access token issuer
    public class CustomTokenService : ITokenService
    {
        protected readonly ILogger Logger;
        protected readonly IHttpContextAccessor Context;
        protected readonly IClaimsService ClaimsProvider;
        protected readonly IReferenceTokenStore ReferenceTokenStore;
        protected readonly ITokenCreationService CreationService;
        protected readonly ISystemClock Clock;
        protected readonly IKeyMaterialService KeyMaterialService;
        protected readonly IdentityServerOptions Options;
        protected readonly IIdPermanenceManager IdPermanenceManager;
        protected readonly IIdSvrRepository IdSvrRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTokenService" /> class.
        /// </summary>
        /// <param name="claimsProvider">The claims provider.</param>
        /// <param name="referenceTokenStore">The reference token store.</param>
        /// <param name="creationService">The signing service.</param>
        /// <param name="contextAccessor">The HTTP context accessor.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="keyMaterialService"></param>
        /// <param name="options">The IdentityServer options</param>
        /// <param name="logger">The logger.</param>
        public CustomTokenService(
            IClaimsService claimsProvider,
            IReferenceTokenStore referenceTokenStore,
            ITokenCreationService creationService,
            IHttpContextAccessor contextAccessor,
            ISystemClock clock,
            IKeyMaterialService keyMaterialService,
            IdentityServerOptions options,
            IIdPermanenceManager idPermanenceManager,
            IIdSvrRepository idSvrRepository,
            ILogger<DefaultTokenService> logger)
        {
            Context = contextAccessor;
            ClaimsProvider = claimsProvider;
            ReferenceTokenStore = referenceTokenStore;
            CreationService = creationService;
            Clock = clock;
            KeyMaterialService = keyMaterialService;
            Options = options;
			IdPermanenceManager = idPermanenceManager;
            IdSvrRepository = idSvrRepository;
			Logger = logger;
        }

        /// <summary>
        /// Creates an identity token.
        /// </summary>
        /// <param name="request">The token creation request.</param>
        /// <returns>
        /// An identity token
        /// </returns>
        public virtual async Task<Token> CreateIdentityTokenAsync(TokenCreationRequest request)
        {
            Logger.LogTrace("Creating identity token");
            request.Validate();

            var credential = await KeyMaterialService.GetSigningCredentialsAsync();
            if (credential == null)
            {
                throw new InvalidOperationException("No signing credential is configured.");
            }

            var signingAlgorithm = credential.Algorithm;

            // host provided claims
            var claims = new List<Claim>();

            // if nonce was sent, must be mirrored in id token
            if (request.Nonce.IsPresent())
            {
                claims.Add(new Claim(JwtClaimTypes.Nonce, request.Nonce));
            }

            // add iat claim
            claims.Add(new Claim(JwtClaimTypes.IssuedAt, Clock.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

            // add at_hash claim
            if (request.AccessTokenToHash.IsPresent())
            {
                claims.Add(new Claim(JwtClaimTypes.AccessTokenHash, CryptoHelper.CreateHashClaimValue(request.AccessTokenToHash, signingAlgorithm)));
            }

            // add c_hash claim
            if (request.AuthorizationCodeToHash.IsPresent())
            {
                claims.Add(new Claim(JwtClaimTypes.AuthorizationCodeHash, CryptoHelper.CreateHashClaimValue(request.AuthorizationCodeToHash, signingAlgorithm)));
            }

            // add s_hash claim
            if (request.StateHash.IsPresent())
            {
                // todo: need constant
                claims.Add(new Claim(JwtClaimTypes.StateHash, request.StateHash));
            }

            // add sid if present
            if (request.ValidatedRequest.SessionId.IsPresent())
            {
                claims.Add(new Claim(JwtClaimTypes.SessionId, request.ValidatedRequest.SessionId));
            }

            claims.AddRange(await ClaimsProvider.GetIdentityTokenClaimsAsync(
                request.Subject,
                request.ValidatedResources,
                request.IncludeAllIdentityClaims,
                request.ValidatedRequest));

            // Run the claims through the ID Permanence.
            var sub = EncryptSubClaim(claims, request.ValidatedRequest, request.Subject);

            // Add user name claims. These should only be added during a token request, not the autorisation request.
            if (IsTokenRequest(request.ValidatedRequest))
            {
                var userInfoClaims = await IdSvrRepository.GetUserInfoClaims(new Guid(DecryptSub(sub, request.ValidatedRequest)));
                if (userInfoClaims != null)
                {
                    claims.Add(new Claim(JwtClaimTypes.Name, userInfoClaims.Name));
                    claims.Add(new Claim(JwtClaimTypes.FamilyName, userInfoClaims.FamilyName));
                    claims.Add(new Claim(JwtClaimTypes.GivenName, userInfoClaims.GivenName));

                    if (userInfoClaims.LastUpdated.HasValue)
                    {
                        claims.Add(new Claim(JwtClaimTypes.UpdatedAt, userInfoClaims.LastUpdated.Value.ToEpoch().ToString(), ClaimValueTypes.Integer));
                    }
                }
            }

            // Set the acr claim to a LoA of 2 - urn:cds.au:cdr:2
            claims.Add(new Claim(StandardClaims.ACR, StandardClaims.ACR2Value));

            var issuer = Context.HttpContext.GetIdentityServerIssuerUri();
            var token = new Token(OidcConstants.TokenTypes.IdentityToken)
            {
                CreationTime = Clock.UtcNow.UtcDateTime,
                Audiences = { request.ValidatedRequest.Client.ClientId },
                Issuer = issuer,
                Lifetime = request.ValidatedRequest.Client.IdentityTokenLifetime,
                Claims = claims.Distinct(new ClaimComparer()).ToList(),
                ClientId = request.ValidatedRequest.Client.ClientId,
                AccessTokenType = request.ValidatedRequest.AccessTokenType
            };

            return token;
        }

		private bool IsTokenRequest(ValidatedRequest request)
		{
            return request is ValidatedTokenRequest;
        }

		/// <summary>
		/// Creates an access token.
		/// </summary>
		/// <param name="request">The token creation request.</param>
		/// <returns>
		/// An access token
		/// </returns>
		public virtual async Task<Token> CreateAccessTokenAsync(TokenCreationRequest request)
        {
            Logger.LogTrace("Creating access token");
            request.Validate();

            var claims = new List<Claim>();
            claims.AddRange(await ClaimsProvider.GetAccessTokenClaimsAsync(
                request.Subject,
                request.ValidatedResources,
                request.ValidatedRequest));

            if (request.ValidatedRequest.Client.IncludeJwtId)
            {
                claims.Add(new Claim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16)));
            }
            var softwreIdClaim = request.ValidatedRequest.ClientClaims.FirstOrDefault(c => c.Type == ClientMetadata.SoftwareId);
            if (softwreIdClaim!= null)
            {
                claims.Add(softwreIdClaim);
            }
            var sectorIdClaim = request.ValidatedRequest.ClientClaims.FirstOrDefault(c => c.Type == ClientMetadata.SectorIdentifierUri);
            if (sectorIdClaim != null)
            {
                claims.Add(sectorIdClaim);
            }

            // If there are selected account Ids, add them to the access token.
            if (request.Subject != null)
            {
                var accountIdsClaims = request.Subject.Claims.Where(c => c.Type == StandardClaims.AccountId);
                if (accountIdsClaims != null && accountIdsClaims.Any())
                {
                    var idParameters = new IdPermanenceParameters
                    {
                        SoftwareProductId = softwreIdClaim?.Value,
                        CustomerId = request.Subject.GetSubjectId()
                    };
                    claims.AddRange(accountIdsClaims.Select(idClaim =>
                        new Claim(idClaim.Type, IdPermanenceManager.EncryptId(idClaim.Value, idParameters), idClaim.ValueType)));
                }
            }

            // Encrypt the sub claim.
            EncryptSubClaim(claims, request.ValidatedRequest, request.Subject);

            var issuer = Context.HttpContext.GetIdentityServerIssuerUri();
            issuer = issuer.Replace("/{0}", string.Empty, StringComparison.CurrentCulture);
            var token = new Token(OidcConstants.TokenTypes.AccessToken)
            {
                CreationTime = Clock.UtcNow.UtcDateTime,
                Issuer = issuer,
                Lifetime = request.ValidatedRequest.AccessTokenLifetime,
                Claims = claims.Distinct(new ClaimComparer()).ToList(),
                ClientId = request.ValidatedRequest.Client.ClientId,
                AccessTokenType = request.ValidatedRequest.AccessTokenType,
                Confirmation = request.ValidatedRequest.Confirmation
            };

            if (Options.EmitStaticAudienceClaim)
            {
                token.Audiences.Add(string.Format(IdentityServerConstants.AccessTokenAudience, issuer.EnsureTrailingSlash()));
            }

            foreach (var api in request.ValidatedResources.Resources.ApiResources)
            {
                if (api.Name.IsPresent())
                {
                    token.Audiences.Add(api.Name);
                }
            }

            return token;
        }

		private string EncryptSubClaim(List<Claim> claims, ValidatedRequest request, ClaimsPrincipal subject)
		{
            // Check if a user and a software product is involved.
            if (subject == null || request.Client == null)
            {
                return null;
            }

            // If the sub claim is not a guid, then no need to encrypt.
            if (!Guid.TryParse(subject.GetSubjectId(), out _))
            {
                return subject.GetSubjectId();
            }

            var subClaim = claims.FirstOrDefault(c => c.Type.Equals(JwtClaimTypes.Subject));
            claims.Remove(subClaim);

            var encryptedSub = EncryptSub(subject.GetSubjectId(), request);
            claims.Add(new Claim(JwtClaimTypes.Subject, encryptedSub));
            return encryptedSub;
		}

        private string EncryptSub(string sub, ValidatedRequest request)
        {
            var param = new SubPermanenceParameters()
            {
                SoftwareProductId = request.Client.Claims.Get(ClientMetadata.SoftwareId),
                SectorIdentifierUri = request.Client.Claims.Get(ClientMetadata.SectorIdentifierUri)
            };
            return IdPermanenceManager.EncryptSub(sub, param);
        }

        private string DecryptSub(string sub, ValidatedRequest request)
        {
            var param = new SubPermanenceParameters()
            {
                SoftwareProductId = request.Client.Claims.Get(ClientMetadata.SoftwareId),
                SectorIdentifierUri = request.Client.Claims.Get(ClientMetadata.SectorIdentifierUri)
            };
            return IdPermanenceManager.DecryptSub(sub, param);
        }

        /// <summary>
        /// Creates a serialized and protected security token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>
        /// A security token in serialized form
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Invalid token type.</exception>
        public virtual async Task<string> CreateSecurityTokenAsync(Token token)
        {
            string tokenResult;

            if (token.Type == OidcConstants.TokenTypes.AccessToken)
            {
                if (token.AccessTokenType == AccessTokenType.Jwt)
                {
                    Logger.LogTrace("Creating JWT access token");

                    tokenResult = await CreationService.CreateTokenAsync(token);
                }
                else
                {
                    Logger.LogTrace("Creating reference access token");

                    var handle = await ReferenceTokenStore.StoreReferenceTokenAsync(token);

                    tokenResult = handle;
                }
            }
            else if (token.Type == OidcConstants.TokenTypes.IdentityToken)
            {
                Logger.LogTrace("Creating JWT identity token");

                tokenResult = await CreationService.CreateTokenAsync(token);
            }
            else
            {
                throw new InvalidOperationException("Invalid token type.");
            }

            return tokenResult;
        }
    }
}
