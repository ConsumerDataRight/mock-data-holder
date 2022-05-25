using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class CustomGrantService : ICustomGrantService
    {
        public enum RemoveGrantsResult
        {
            OK,
            GrantNotValid,
            GrantNotAssociatedToClient,
            Error
        }

        private readonly ILogger _logger;
        private readonly IPersistedGrantStore _persistedGrantStore;

        public CustomGrantService(
            ILogger<CustomGrantService> logger,
            IPersistedGrantStore persistedGrantStore)
        {
            _persistedGrantStore = persistedGrantStore;
            _logger = logger;
        }

        /// <summary>
        /// Get grant via key id.
        /// </summary>
        /// <param name="keyId">KeyId.</param>
        /// <returns>PersistedGrant item.</returns>
        public async Task<PersistedGrant> GetGrant(string keyId)
        {
            return await _persistedGrantStore.GetAsync(keyId);
        }

        /// <summary>
        /// Get grant via key id, subject id and grant type.
        /// </summary>
        /// <param name="keyId">KeyId.</param>
        /// <param name="subjectId">Subject ID.</param>
        /// <param name="grantType">Grant Type.</param>
        /// <returns>PersistedGrant item.</returns>
        public async Task<PersistedGrant> GetGrant(string keyId, string subjectId, string grantType)
        {
            // Find the grant by key.
            var grant = await _persistedGrantStore.GetAsync(keyId);

            if (grant == null)
            {
                return null;
            }

            // Then if all other values match, return the grant.
            if (grant.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase) && grant.Type.Equals(grantType, StringComparison.OrdinalIgnoreCase))
            {
                return grant;
            }

            // Otherwise return null.
            return null;
        }

        /// <summary>
        /// Get grant via subjectId, grant type and keyword in data column.
        /// </summary>
        /// <param name="subjectId">subjectId.</param>
        /// <param name="grantType">grant type (default value is cdr_arrangement_grant).</param>
        /// <param name="keywordInData">keyword filter which is contained in data column.</param>
        /// <returns>PersistedGrant item.</returns>
        public async Task<PersistedGrant> GetGrantByKeyword(string subjectId, string grantType, string keywordInData)
        {
            var grants = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { SubjectId = subjectId });
            return grants.FirstOrDefault(g => g.Type == grantType && g.Data.Contains(keywordInData, StringComparison.OrdinalIgnoreCase) &&
                                                              (!g.Expiration.HasValue ||
                                                               (g.Expiration.HasValue && ((DateTime)g.Expiration).CompareTo(DateTime.UtcNow) > 0)
                                                              )
                                                         );
        }

        /// <summary>
        /// Store a persistedGrant object into database.
        /// </summary>
        /// <param name="persistedGrant">persistedGrant.</param>
        /// <returns>Grant item key.</returns>
        public async Task<string> StoreGrant(PersistedGrant persistedGrant)
        {
            await _persistedGrantStore.StoreAsync(persistedGrant);
            return persistedGrant.Key;
        }

        /// <summary>
        /// Removes grants found for the (cdr arrangement id). Currently this is just the refresh token.
        /// </summary>
        /// <returns>
        /// </returns>
        public async Task<RemoveGrantsResult> RemoveGrantsForCdrArrangementId(string cdrArrangementId, string clientId)
        {
            (var cdrArrangementGrant, bool isValidClient, bool isValidArrangement) = await GetCdrArrangementGrant(cdrArrangementId, clientId);

            if (cdrArrangementGrant == null && !isValidClient && !isValidArrangement)
            {
                _logger.LogError("The CdrArrangementId: {CdrArrangementId} is not valid for the given ClientId: {ClientId}", cdrArrangementId, clientId);
                return RemoveGrantsResult.GrantNotValid;
            }

            if (cdrArrangementGrant == null && isValidClient && !isValidArrangement)
            {
                _logger.LogError("The CdrArrangementId: {CdrArrangementId} provided is not associated with ClientId: {ClientId}", cdrArrangementId, clientId);
                return RemoveGrantsResult.GrantNotAssociatedToClient;
            }

            if (cdrArrangementGrant != null && isValidClient && isValidArrangement)
            {
                var cdrArrangementIdData = JsonConvert.DeserializeObject<CdrArrangementGrant>(cdrArrangementGrant.Data);
                if (string.IsNullOrWhiteSpace(cdrArrangementIdData.RefreshTokenKey))
                {
                    _logger.LogInformation("The CDR Arrangment Grant {CdrArrangementId} does not have a refresh token to revoke", cdrArrangementId);
                    return RemoveGrantsResult.OK;
                }

                var refreshTokenGrant = await _persistedGrantStore.GetAsync(cdrArrangementIdData.RefreshTokenKey);

                if (refreshTokenGrant == null || string.IsNullOrWhiteSpace(refreshTokenGrant.Data))
                {
                    _logger.LogInformation("The CdrArrangementId: {CdrArrangementId} provided did not find an associated refresh token to revoke", cdrArrangementId);
                    return RemoveGrantsResult.OK;
                }

                await RemoveGrant(cdrArrangementIdData.RefreshTokenKey);
                _logger.LogInformation("Revoked RefreshToken: {RefreshTokenKey} using CdrArrangementId: {CdrArrangementId}", cdrArrangementIdData.RefreshTokenKey, cdrArrangementId);
                return RemoveGrantsResult.OK;
            }

            _logger.LogError("GENERAL ERROR: CdrArrangementId: {CdrArrangementId}, ClientId: {ClientId}", cdrArrangementId, clientId);
            return RemoveGrantsResult.Error;
        }

        public async Task RemoveGrant(string key)
        {
            await _persistedGrantStore.RemoveAsync(key);
            _logger.LogInformation("Removed key from grant store: {key}", key);
        }

        public async Task<string> GetAlternativeCdrArrangementIdFromSubjectGrants(string cdrArrangementId, string clientId)
        {
            (var cdrArrangementGrant, bool isValidClient, bool isValidArrangement) = await GetCdrArrangementGrant(cdrArrangementId, clientId);
            if (cdrArrangementGrant != null)
            {
                var cdrArrangementIdData = JsonConvert.DeserializeObject<CdrArrangementGrant>(cdrArrangementGrant.Data);

                var cdrArrangementGrants = await GetGrants(cdrArrangementIdData.Subject, CdsConstants.GrantTypes.CdrArrangementGrant);
                return cdrArrangementGrants.FirstOrDefault(x => x.Key != cdrArrangementId)?.Key;
            }

            return null;
        }

        public async Task<bool> UpdateCdrArrangementGrant(string cdrArrangementId, string authCode)
        {
            var grant = await _persistedGrantStore.GetAsync(cdrArrangementId);
            var cdrArrangementData = JsonConvert.DeserializeObject<CdrArrangementGrant>(grant.Data);

            cdrArrangementData.AuthCode = authCode;
            cdrArrangementData.RefreshTokenKey = string.Empty;
            var data = JsonConvert.SerializeObject(cdrArrangementData, Formatting.Indented);
            grant.Data = data;

            await _persistedGrantStore.StoreAsync(grant);
            _logger.LogInformation("Update CdrArrangmentGrant AuthCode for CdrArrangmentId: {cdrArrangementId}", cdrArrangementId);

            return true;
        }

        private async Task<List<PersistedGrant>> GetGrants(string subjectId, string grantType)
        {
            var grants = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter() { SubjectId = subjectId });
            return grants.Where(g => g.Type == grantType).ToList();
        }

        private async Task<(PersistedGrant, bool, bool)> GetCdrArrangementGrant(string cdrArrangementId, string clientId)
        {
            var cdrArrangementGrant = await _persistedGrantStore.GetAsync(cdrArrangementId);
            if (cdrArrangementGrant != null && !string.IsNullOrWhiteSpace(cdrArrangementGrant.Data) && cdrArrangementGrant.ClientId == clientId)
            {
                return (cdrArrangementGrant, true, true);
            }
            else if (cdrArrangementGrant != null && !string.IsNullOrWhiteSpace(cdrArrangementGrant.Data) && cdrArrangementGrant.ClientId != clientId)
            {
                return (null, true, false);
            }
            return (null, false, false);
        }
    }
}
