using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US15221_US12969_US15585_MDH_InfosecProfileAPI_Registration_PUT : BaseTest, IClassFixture<TestFixture>
    {
        // Purge database, register product and return SSA JWT and registration json
        static private async Task<(string ssa, string registration)> Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer();
            return await TestSetup.DataHolder_RegisterSoftwareProduct();
        }

        [Fact]
        public async Task AC11_Put_WithValidSoftwareProduct_ShouldRespondWith_200OK_UpdatedProfile()
        {
            // Arrange
            var (ssa, expectedResponse) = await Arrange();

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);

            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = await new DataHolderAccessToken().GetAccessToken(),
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Assert - Check json
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC12_Put_WithInvalidSoftwareProduct_ShouldRespondWith_400BadRequest_InvalidErrorResponse()
        {
            // Arrange
            var (ssa, _) = await Arrange();

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa,
                redirect_uris: new string[] { "foo" });  // Invalid redirect uris

            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = await new DataHolderAccessToken().GetAccessToken(),
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var expectedResponse = @"{
                        ""error"": ""invalid_redirect_uri"",
                        ""error_description"": ""One or more redirect uri is invalid""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC13_Put_WithExpiredAccessToken_ShouldRespondWith_401UnAuthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var (ssa, _) = await Arrange();

            var accessToken = await new DataHolderAccessToken().GetAccessToken(true);

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{SOFTWAREPRODUCT_ID.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = accessToken,
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
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

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID_INVALID)]
        public async Task AC14_Put_WithInvalidOrUnregisteredClientID_ShouldRespondWith_401Unauthorised_InvalidErrorResponse(string softwareProductId)
        {
            // Arrange
            var (ssa, _) = await Arrange();

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);

            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{softwareProductId.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = await new DataHolderAccessToken().GetAccessToken(),
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
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
                    Assert_HasHeader(@"Bearer error=""invalid_request"", error_description=""The client is unknown.""",
                       response.Headers, "WWW-Authenticate");
                }
            }
        }
    }
}