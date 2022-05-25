using CDR.DataHolder.IntegrationTests.Extensions;
using CDR.DataHolder.IntegrationTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US15221_US12969_US15586_MDH_InfosecProfileAPI_Registration_GET : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        [Fact]
        public async Task AC08_Get_WithValidClientId_ShouldRespondWith_200OK_Profile()
        {
            // Arrange
            var accessToken = await new DataHolderAccessToken().GetAccessToken();

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task AC09_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var accessToken = await new DataHolderAccessToken().GetAccessToken(true);

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Assert - Check WWWAutheticate header
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at '05/16/2022 03:04:03'""",
                        response.Headers, "WWW-Authenticate");
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.OK)]
        [InlineData(SOFTWAREPRODUCT_ID_INVALID, HttpStatusCode.Unauthorized)]
        public async Task AC10_Get_WithInvalidClientId_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(string softwareProductId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = await new DataHolderAccessToken().GetAccessToken();

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{softwareProductId.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                AccessToken = accessToken
            };
            var response = await api.SendAsync(AllowAutoRedirect: false);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check error response 
                    Assert_HasHeader(@"Bearer error=""invalid_request"", error_description=""The client is unknown.""",
                        response.Headers,
                        "WWW-Authenticate",
                        true); // starts with
                }
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.Unauthorized)]
        public async Task AC10b_Get_WithInvalidClientId_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(bool hideSoftwareProduct, HttpStatusCode expectedStatusCode)
        {
            static void HideSoftwareProduct()
            {
                // Hide in identity server
                using (var connection = new SqlConnection(IDENTITYSERVER_CONNECTIONSTRING))
                {
                    connection.Open();
                    connection.ExecuteNonQuery($@"update clients set clientid = 'foo { SOFTWAREPRODUCT_ID.ToLower() }' where clientid = '{ SOFTWAREPRODUCT_ID.ToLower() }'");
                    if (connection.ExecuteScalarInt32($@"select count(*) from clients where clientid = '{ SOFTWAREPRODUCT_ID.ToLower() }'") != 0) throw new Exception("Error hiding softwareproduct - idsvr");
                }

                // Hide in dataholder
                using (var connection = new SqlConnection(DATAHOLDER_CONNECTIONSTRING))
                {
                    connection.Open();
                    connection.ExecuteNonQuery($@"DELETE FROM softwareproduct WHERE softwareproductid = '{ SOFTWAREPRODUCT_ID }'");
                    if (connection.ExecuteScalarInt32($@"select count(*) from softwareproduct where softwareproductid = '{ SOFTWAREPRODUCT_ID }'") != 0) throw new Exception("Error hiding softwareproduct -  mdh");
                }
            }

            static void RestoreSoftwareProduct()
            {
                // Restore in identity server
                using (var connection = new SqlConnection(IDENTITYSERVER_CONNECTIONSTRING))
                {
                    connection.Open();
                    connection.ExecuteNonQuery($@"update clients set clientid = '{ SOFTWAREPRODUCT_ID.ToLower() }' where clientid = 'foo { SOFTWAREPRODUCT_ID.ToLower() }'");
                    if (connection.ExecuteScalarInt32($@"select count(*) from clients where clientid = '{ SOFTWAREPRODUCT_ID.ToLower() }'") != 1) throw new Exception("Error restoring softwareproduct - idsvr");
                }

                // Restore in dataholder
                using (var connection = new SqlConnection(DATAHOLDER_CONNECTIONSTRING))
                {
                    connection.Open();
                    string sqlQuery = string.Format("INSERT INTO [dbo].[SoftwareProduct]([SoftwareProductId],[SoftwareProductName],[SoftwareProductDescription],[LogoUri],[Status],[BrandId]) VALUES ('{0}','MyBudgetHelper',NULL,'https://mocksoftware/mybudgetapp/img/logo.png','ACTIVE','{1}')", SOFTWAREPRODUCT_ID, BRANDID);
                    connection.ExecuteNonQuery(sqlQuery);
                    if (connection.ExecuteScalarInt32($@"select count(*) from softwareproduct where softwareproductid = '{ SOFTWAREPRODUCT_ID }'") != 1) throw new Exception("Error restoring softwareproduct -  mdh");
                }
            }

            // Arrange - Get access token from DH
            var accessToken = await new DataHolderAccessToken().GetAccessToken();

            // Arrange - Hide software product in DH and IDSvr database
            if (hideSoftwareProduct)
            {
                HideSoftwareProduct();
            }
            try
            {
                // Act - Attempting to get registration should fail since DH has no record of software product
                var api = new Infrastructure.API
                {
                    URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    AccessToken = accessToken
                };
                var response = await api.SendAsync(AllowAutoRedirect: false);

                // Assert
                using (new AssertionScope())
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedStatusCode);

                    // Assert - Check error response
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        // Assert - Check error response 
                        Assert_HasHeader(@"Bearer error=""invalid_request"", error_description=""The client is unknown.""",
                            response.Headers,
                            "WWW-Authenticate",
                            true); // starts with
                    }
                }
            }
            finally
            {
                if (hideSoftwareProduct)
                {
                    RestoreSoftwareProduct();
                }
            }
        }
    }   
}