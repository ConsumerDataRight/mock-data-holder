using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using static IdentityModel.OidcConstants;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_Authorise_API
    {
        /// <summary>
        /// "Fake" authorisation consent flow by creating persited grants
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="scope"></param>
        /// <param name="sharingDuration"></param>
        /// <param name="lifetimeSeconds"></param>
        /// <returns>AuthCode and CdrArrangementId</returns>
        static public (string authCode, string cdrArrangementId) Authorise(
            string customerId, 
            string? scope, 
            int? sharingDuration = null, 
            int lifetimeSeconds = 600, 
            string[]? accountIds = null)
        {
            // Insert userconsent into persistedgrants table
            static void InsertUserConsent(SqlConnection connection, string subject, string? scope)
            {
                var key = Guid.NewGuid().ToString();

                var creationTime = DateTime.UtcNow;

                var data = new
                {
                    // refresh_token_key = "foo",
                    SubjectId = subject,
                    ClientId = BaseTest.SOFTWAREPRODUCT_ID.ToLower(),
                    Scopes = scope?.Split(' '),
                    CreationTime = creationTime,
                    Expiration = (DateTime?)null
                };

                using var insertCommand = new SqlCommand($@"
                    insert into persistedgrants ([type], [clientId], [creationTime], [subjectId], [data], [key])
                    values (@type, @clientId, @creationTime, @subjectId, @data, @key)",
                    connection);

                insertCommand.Parameters.AddWithValue("@type", "user_consent");
                insertCommand.Parameters.AddWithValue("@clientId", BaseTest.SOFTWAREPRODUCT_ID.ToLower());
                insertCommand.Parameters.AddWithValue("@creationTime", creationTime);
                insertCommand.Parameters.AddWithValue("@subjectId", subject);
                insertCommand.Parameters.AddWithValue("@data", data.ToJson());
                insertCommand.Parameters.AddWithValue("@key", key);

                insertCommand.ExecuteNonQuery();
            }

            // Insert cdr arrangement into persistedgrants table
            static string InsertCDRArrangement(SqlConnection connection, string subject)
            {
                var cdrArrangementId = Guid.NewGuid().ToString();
                var creationTime = DateTime.UtcNow;

                var data = new
                {
                    subject,
                };

                using var insertCommand = new SqlCommand($@"
                    insert into persistedgrants ([type], [clientId], [creationTime], [subjectId], [data], [key])
                    values (@type, @clientId, @creationTime, @subjectId, @data, @key)",
                    connection);

                insertCommand.Parameters.AddWithValue("@type", "cdr_arrangement_grant");
                insertCommand.Parameters.AddWithValue("@clientId", BaseTest.SOFTWAREPRODUCT_ID.ToLower());
                insertCommand.Parameters.AddWithValue("@creationTime", creationTime);
                insertCommand.Parameters.AddWithValue("@subjectId", subject);
                insertCommand.Parameters.AddWithValue("@data", data.ToJson());
                insertCommand.Parameters.AddWithValue("@key", cdrArrangementId);

                insertCommand.ExecuteNonQuery();

                return cdrArrangementId;
            }

            // Insert authorization into persistedgrants table
            static string InsertAuthorizationCode(
                SqlConnection connection, string subject, string? scope, int? sharingDuration, int lifetimeSeconds, string cdrArrangementId, string[]? accountIds)
            {
                // var authCode = BaseTest.AUTHORISATION_CODE;
                var authCode = Guid.NewGuid().ToString();

                var SOFTWAREPRODUCT_ID = BaseTest.SOFTWAREPRODUCT_ID.ToLower();
                var RedirectUris = new string[] { BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS };

                // Key for persisted grant
                var key = $"{authCode}:authorization_code".Sha256();

                // Delete persisted grant 
                // using var deleteCommand = new SqliteCommand($"delete from persistedgrants where key = @key", connection);
                // deleteCommand.Parameters.AddWithValue("@key", key);
                // deleteCommand.ExecuteNonQuery();

                // Create persisted grant
                var creationTime = DateTime.UtcNow;
                var expiration = creationTime.AddSeconds(lifetimeSeconds);

                // if (expired.HasValue && expired.Value)
                // {
                //     creationTime = DateTime.UtcNow.AddMinutes(-12);
                //     expiration = DateTime.UtcNow.AddMinutes(-2);
                // }

                using var insertCommand = new SqlCommand($"insert into persistedgrants ([type], [clientId], [creationTime], [subjectId], [data], [expiration], [key]) values (@type, @clientId, @creationTime, @subjectId, @data, @expiration, @key)", connection);
                insertCommand.Parameters.AddWithValue("@type", "authorization_code");
                insertCommand.Parameters.AddWithValue("@clientId", SOFTWAREPRODUCT_ID);
                insertCommand.Parameters.AddWithValue("@creationTime", creationTime);
                insertCommand.Parameters.AddWithValue("@subjectId", subject);
                insertCommand.Parameters.AddWithValue("@expiration", expiration);
                insertCommand.Parameters.AddWithValue("@key", key);

                var additionalClaims = new List<Claim>
                {
                    new Claim("acr", "urn:cds.au:cdr:2"),
                    new Claim("cdr_arrangement_id", cdrArrangementId)
                };

                if (accountIds != null && accountIds.Length > 0)
                {
                    additionalClaims.AddRange(accountIds.Select(id => new Claim("account_id", id)));
                }

                if (sharingDuration != null)
                {
                    var shareExpiration = creationTime.AddSeconds(sharingDuration.Value);
                    additionalClaims.Add(new Claim("sharing_expires_at", $"{new DateTimeOffset(shareExpiration).ToUnixTimeSeconds()}", ClaimValueTypes.Integer));
                }
                // else
                // {
                //     additionalClaims.Add(new Claim("sharing_expires_at", $"{new DateTimeOffset(expiration).ToUnixTimeSeconds()}", ClaimValueTypes.Integer));
                // }

                var data = new AuthorizationCode
                {
                    ClientId = SOFTWAREPRODUCT_ID,
                    CreationTime = creationTime,
                    Lifetime = lifetimeSeconds,
                    RequestedScopes = scope?.Split(' '),
                    Subject = new IdentityServerUser(subject)
                    {
                        AuthenticationTime = DateTime.UtcNow,
                        IdentityProvider = "ntt-bank",
                        AuthenticationMethods = new string[] { AuthenticationMethods.OneTimePassword },
                        AdditionalClaims = additionalClaims,
                    }.CreatePrincipal(),
                    IsOpenId = true,
                    RedirectUri = RedirectUris[0],
                };
                insertCommand.Parameters.AddWithValue("@data", data.ToJson());

                insertCommand.ExecuteNonQuery();

                return authCode;
            }

            // Connect to IdentityServer db
            using var connection = new SqlConnection(BaseTest.IDENTITYSERVER_CONNECTIONSTRING);
            connection.Open();

            InsertUserConsent(connection, customerId, scope);
            var cdrArrangementId = InsertCDRArrangement(connection, customerId);
            var authCode = InsertAuthorizationCode(connection, customerId, scope, sharingDuration, lifetimeSeconds, cdrArrangementId, accountIds);

            return (authCode, cdrArrangementId);
        }
    }
}
