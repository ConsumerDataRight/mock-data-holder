using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace CDR.DataHolder.IntegrationTests
{
    public class US17613_MDH_DiscoveryAPI_GetOutages : BaseTest
    {
        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_Outages()
        {
            // Arrange
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                XV = "1",
                URL = $"{DH_TLS_PUBLIC_BASE_URL}/cds-au/v1/discovery/outages"
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
                        ""outages"": [{
                                ""outageTime"": ""2021-06-30T23:00:00Z"",
                                ""duration"": ""PT3H30M"",
                                ""isPartial"": false,
                                ""explanation"": ""Scheduled maintenance""
                            },
                            {
                                ""outageTime"": ""2021-07-22T19:30:00Z"",
                                ""duration"": ""PT1H"",
                                ""isPartial"": false,
                                ""explanation"": ""System Upgrade""
                            },
                            {
                                ""outageTime"": ""2021-08-30T23:00:00Z"",
                                ""duration"": ""PT2H15M"",
                                ""isPartial"": true,
                                ""explanation"": ""Server Patching""
                            }
                        ]
                    },
                    ""links"": {
                        ""self"": ""#{public_base_uri}/cds-au/v1/discovery/outages""
                    },
                    ""meta"": {}
                }".Replace("#{public_base_uri}", DH_TLS_PUBLIC_BASE_URL);
                
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }
    }
}
