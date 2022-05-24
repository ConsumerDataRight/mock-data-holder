using CDR.DataHolder.IntegrationTests.Extensions;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using Microsoft.Data.SqlClient;
using System;
using System.Net;
using System.Threading.Tasks;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Fixtures
{
    /// <summary>
    /// Methods for setting up tests
    /// </summary>
    static class TestSetup
    {
        /// <summary>
        /// The seed data for the Register is using the loopback uri for redirecturi.
        /// Since the integration tests stands up it's own data recipient consent/callback endpoint we need to 
        /// patch the redirect uri to match our callback.
        /// </summary>
        static public void Register_PatchRedirectUri(
            string softwareProductId = BaseTest.SOFTWAREPRODUCT_ID,
            string redirectURI = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS)
        {
            redirectURI = BaseTest.SubstituteConstant(redirectURI);

            using var connection = new SqlConnection(BaseTest.REGISTER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqlCommand("update softwareproduct set redirecturis = @uri where lower(softwareproductid) = @id", connection);
            updateCommand.Parameters.AddWithValue("@uri", redirectURI);
            updateCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            updateCommand.ExecuteNonQuery();

            using var selectCommand = new SqlCommand($"select redirecturis from softwareproduct where lower(softwareproductid) = @id", connection);
            selectCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            if (selectCommand.ExecuteScalarString() != redirectURI)
            {
                throw new Exception($"softwareproduct.redirecturis is not '{redirectURI}'");
            }
        }

        /// <summary>
        /// The seed data for the Register is using the loopback uri for jwksuri.
        /// Since the integration tests stands up it's own data recipient jwks endpoint we need to 
        /// patch the jwks uri to match our endpoint.
        /// </summary>
        static public void Register_PatchJwksUri(
            string softwareProductId = BaseTest.SOFTWAREPRODUCT_ID,
            string jwksURI = BaseTest.SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS)
        {
            jwksURI = BaseTest.SubstituteConstant(jwksURI);

            using var connection = new SqlConnection(BaseTest.REGISTER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqlCommand("update softwareproduct set jwksuri = @uri where lower(softwareproductid) = @id", connection);
            updateCommand.Parameters.AddWithValue("@uri", jwksURI);
            updateCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            updateCommand.ExecuteNonQuery();

            using var selectCommand = new SqlCommand($"select jwksuri from softwareproduct where lower(softwareproductid) = @id", connection);
            selectCommand.Parameters.AddWithValue("@id", softwareProductId.ToLower());
            if (selectCommand.ExecuteScalarString() != jwksURI)
            {
                throw new Exception($"softwareproduct.jwksuri is not '{jwksURI}'");
            }
        }

        /// <summary>
        /// Clear data from the Dataholder's IdentityServer database
        /// </summary>
        /// <param name="onlyPersistedGrants">Only clear the persisted grants table</param>
        static public void DataHolder_PurgeIdentityServer(bool onlyPersistedGrants = false)
        {
            using var connection = new SqlConnection(BaseTest.IDENTITYSERVER_CONNECTIONSTRING);

            void Purge(string table)
            {
                // Delete all rows
                using var deleteCommand = new SqlCommand($"delete from {table}", connection);
                deleteCommand.ExecuteNonQuery();

                // Check all rows deleted
                using var selectCommand = new SqlCommand($"select count(*) from {table}", connection);
                var count = selectCommand.ExecuteScalarInt32();
                if (count != 0)
                {
                    throw new Exception($"Table {table} was not purged");
                }
            }

            connection.Open();

            if (!onlyPersistedGrants)
            {
                Purge("ApiResourceClaims");
                Purge("ApiResourceProperties");
                Purge("ApiResources");
                Purge("ApiResourceScopes");
                Purge("ApiResourceSecrets");
                Purge("ApiScopeClaims");
                Purge("ApiScopeProperties");
                Purge("ApiScopes");
                Purge("ClientClaims");
                Purge("ClientCorsOrigins");
                Purge("ClientGrantTypes");
                Purge("ClientIdPRestrictions");
                Purge("ClientPostLogoutRedirectUris");
                Purge("ClientProperties");
                Purge("ClientRedirectUris");
                Purge("Clients");
                Purge("ClientScopes");
                Purge("ClientSecrets");
                Purge("DeviceCodes");
                Purge("IdentityResourceClaims");
                Purge("IdentityResourceProperties");
                Purge("IdentityResources");
            }

            Purge("PersistedGrants");
        }

        // Get SSA from the Register and register it with the DataHolder
        static public async Task<(string ssa, string registration)> DataHolder_RegisterSoftwareProduct(
            string brandId = BaseTest.BRANDID,
            string softwareProductId = BaseTest.SOFTWAREPRODUCT_ID,
            string jwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string jwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            // Get SSA from Register
            var ssa = await Register_SSA_API.GetSSA(brandId, softwareProductId, "3", jwtCertificateFilename, jwtCertificatePassword);

            // Register software product with DataHolder
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa,
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword);

            // var response = await US15221_US12969_US15586_MDH_InfosecProfileAPI_Registration_Base.RegisterSoftwareProduct(registrationRequest);
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Unable to register software product - { softwareProductId }");
            }

            var registration = await response.Content.ReadAsStringAsync();

            return (ssa, registration);
        }
    }
}
