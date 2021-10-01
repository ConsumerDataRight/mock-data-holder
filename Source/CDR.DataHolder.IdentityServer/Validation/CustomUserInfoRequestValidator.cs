using System;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.Domain.Repositories;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class CustomUserInfoRequestValidator : IUserInfoRequestValidator
    {
        private readonly ITokenValidator _tokenValidator;
        private readonly IProfileService _profile;
        private readonly ILogger _logger;
        private readonly IStatusRepository _statusRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomUserInfoRequestValidator" /> class.
        /// </summary>
        /// <param name="tokenValidator">The token validator.</param>
        /// <param name="profile">The profile service</param>
        /// <param name="logger">The logger.</param>
        public CustomUserInfoRequestValidator(
            ITokenValidator tokenValidator,
            IProfileService profile,
            ILogger<CustomUserInfoRequestValidator> logger,
            IStatusRepository statusRepository)
        {
            _tokenValidator = tokenValidator;
            _profile = profile;
            _logger = logger;
            _statusRepository = statusRepository;
        }

        /// <summary>
        /// Validates a userinfo request.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task<UserInfoRequestValidationResult> ValidateRequestAsync(string accessToken)
        {
            // the access token needs to be valid and have at least the openid scope
            var tokenResult = await _tokenValidator.ValidateAccessTokenAsync(
                accessToken,
                IdentityServerConstants.StandardScopes.OpenId);

            if (tokenResult.IsError)
            {
                return new UserInfoRequestValidationResult
                {
                    IsError = true,
                    Error = tokenResult.Error
                };
            }

            // the token must have a one sub claim
            var subClaim = tokenResult.Claims.SingleOrDefault(c => c.Type == JwtClaimTypes.Subject);
            if (subClaim == null)
            {
                _logger.LogError("Token contains no sub claim");

                return new UserInfoRequestValidationResult
                {
                    IsError = true,
                    Error = OidcConstants.ProtectedResourceErrors.InvalidToken
                };
            }

            // create subject from incoming access token
            //var claims = tokenResult.Claims.Where(x => !IdentityServerConstants.Filters.ProtocolClaimsFilter.Contains(x.Type));
            var claims = tokenResult.Claims;
            var subject = Principal.Create("UserInfo", claims.ToArray());

            // make sure user is still active
            var isActiveContext = new IsActiveContext(subject, tokenResult.Client, IdentityServerConstants.ProfileIsActiveCallers.UserInfoRequestValidation);
            await _profile.IsActiveAsync(isActiveContext);

            if (isActiveContext.IsActive == false)
            {
                _logger.LogError("User is not active: {sub}", subject.GetSubjectId());

                return new UserInfoRequestValidationResult
                {
                    IsError = true,
                    Error = OidcConstants.ProtectedResourceErrors.InvalidToken
                };
            }

            // Validate the SP statuses                        
            var softwareProductId = subject.GetSoftwareProductId();            
            var legalEntityStatus = await InvalidLegalEntityStatus(softwareProductId);
            var sofwareProductStatus = await InvalidSoftwareProductStatus(softwareProductId);
            var error = string.Empty;

            if (!string.IsNullOrEmpty(legalEntityStatus))
            {
                _logger.LogError("LegalEntity status is not active: {spId}", softwareProductId);
                
                if (string.Equals(legalEntityStatus, UserInfoStatusErrorDescriptions.StatusInactive, StringComparison.OrdinalIgnoreCase))
                {
                    error = UserInfoErrorCodes.InvalidLegalStatusInactive;
                }
                else if (string.Equals(legalEntityStatus, UserInfoStatusErrorDescriptions.StatusRemoved, StringComparison.OrdinalIgnoreCase))
                {
                    error = UserInfoErrorCodes.InvalidLegalStatusRemoved;
                }
                else if (string.Equals(legalEntityStatus, UserInfoStatusErrorDescriptions.StatusRevoked, StringComparison.OrdinalIgnoreCase))
                {
                    error = UserInfoErrorCodes.InvalidLegalStatusRevoked;
                }
                else if (string.Equals(legalEntityStatus, UserInfoStatusErrorDescriptions.StatusSurrendered, StringComparison.OrdinalIgnoreCase))
                {
                    error = UserInfoErrorCodes.InvalidLegalStatusSurrendered;
                }
                else if (string.Equals(legalEntityStatus, UserInfoStatusErrorDescriptions.StatusSuspended, StringComparison.OrdinalIgnoreCase))
                {
                    error = UserInfoErrorCodes.InvalidLegalStatusSuspended;
                }                
            }
            else if (!string.IsNullOrEmpty(sofwareProductStatus))
            {
                _logger.LogError("sofwareProduct status is not active: {spId}", softwareProductId);

                if (string.Equals(sofwareProductStatus, UserInfoStatusErrorDescriptions.StatusInactive, StringComparison.OrdinalIgnoreCase))
                {
                    error = UserInfoErrorCodes.InvalidSoftwareProductStatusInactive;
                }
                else if (string.Equals(sofwareProductStatus, UserInfoStatusErrorDescriptions.StatusRemoved, StringComparison.OrdinalIgnoreCase))
                {
                    error = UserInfoErrorCodes.InvalidSoftwareProductStatusRemoved;
                }                                
            }

            if (!string.IsNullOrEmpty(error))
            {
                return new UserInfoRequestValidationResult
                {
                    IsError = true,
                    Error = error
                };
            }
            
            return new UserInfoRequestValidationResult
            {
                IsError = false,
                TokenValidationResult = tokenResult,
                Subject = subject
            };
        }

        private async Task<string> InvalidLegalEntityStatus(string softwareProductId)
        {
            if (!string.IsNullOrEmpty(softwareProductId))
            {
                var softwareProduct = await _statusRepository.GetSoftwareProduct(Guid.Parse(softwareProductId));
                var legalEntityStatus = softwareProduct?.Brand?.LegalEntity?.Status;

                if (!string.Equals(legalEntityStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return legalEntityStatus;
                }
            }
            return string.Empty;
        }

        private async Task<string> InvalidSoftwareProductStatus(string softwareProductId)
        {
            if (!string.IsNullOrEmpty(softwareProductId))
            {
                var softwareProduct = await _statusRepository.GetSoftwareProduct(Guid.Parse(softwareProductId));
                var status = softwareProduct?.Status;

                if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return status;
                }
            }
            return string.Empty;
        }
    }
}
