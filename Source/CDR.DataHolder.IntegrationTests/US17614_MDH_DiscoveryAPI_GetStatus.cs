using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace CDR.DataHolder.IntegrationTests
{
    public class US17614_MDH_DiscoveryAPI_GetStatus : BaseTest
    {
        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_Status()
        {
            // Arrange
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                XV = "1",
                URL = $"{DH_TLS_PUBLIC_BASE_URL}/cds-au/v1/discovery/status"
            };

            // Act
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var expectedResponse = @"{
                    ""data"": {
                        ""status"": ""OK"",
                        ""explanation"": """",
                        ""detectionTime"": """",
                        ""expectedResolutionTime"": """",
                        ""updateTime"": """"
                    },
                    ""links"": {
                        ""self"": ""#{public_base_uri}/cds-au/v1/discovery/status""
                    },
                    ""meta"": {}
                }".Replace("#{public_base_uri}", DH_TLS_PUBLIC_BASE_URL);
                
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }
    }
}
