using Xunit;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using CDR.DataHolder.IntegrationTests.Fixtures;

namespace CDR.DataHolder.IntegrationTests
{
    public class US15221_US12969_US15587_MDH_InfosecProfileAPI_Registration_DELETE : BaseTest, IClassFixture<TestFixture>
    {
        // Purge database, register product and return SSA JWT and registration json
        static private async Task<(string ssa, string registration)> Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer();
            return await TestSetup.DataHolder_RegisterSoftwareProduct();
        }

        [Fact]
        public async Task AC15_Delete_WithValidClientId_ShouldRespondWith_204NoContent_ProfileIsDeleted()
        {
            // Arrange
            await Arrange();

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Delete,
                // AccessToken = GetAccessToken(),
                AccessToken = await new DataHolderAccessToken().GetAccessToken(),
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);

                await Assert_HasNoContent2(response.Content);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    // TODO - do a get, should fail
                }
            }
        }

        // AC16 deleted
        // [Fact]
        // public void AC16_Delete_WithDeletedClientId_ShouldRespondWith_405MethodNotAllowed_ProfileIsDeleted()
        // {
        // #if DEBUG
        //     throw new NotImplementedException();
        // #endif
        // }

        [Fact]
        public async Task AC17_Delete_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            await Arrange();

            var accessToken = await new DataHolderAccessToken().GetAccessToken(true);

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Delete,
                AccessToken = accessToken,
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Assert - Check WWWAutheticate header
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at '06/16/2021 05:16:01'""",
                        response.Headers, "WWW-Authenticate");
                }
            }
        }
    }
}