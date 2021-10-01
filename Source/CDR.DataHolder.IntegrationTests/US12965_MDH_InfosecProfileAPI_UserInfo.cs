using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US12965_MDH_InfosecProfileAPI_UserInfo : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private RegisterSoftwareProductFixture Fixture { get; init; }

        public US12965_MDH_InfosecProfileAPI_UserInfo(RegisterSoftwareProductFixture fixture)
        {
            Fixture = fixture;
        }

        class AC01_AC02_Expected
        {
#pragma warning disable IDE1006                
            public string? given_name { get; set; }
            public string? family_name { get; set; }
            public string? name { get; set; }
            public string? sub { get; set; }
            public string? iss { get; set; }
            public string? aud { get; set; }
#pragma warning restore IDE1006                
        }

        private async Task Test_AC01_AC02(HttpMethod httpMethod, TokenType tokenType, string expectedName, string expectedGivenName, string expectedFamilyName)
        {
            // Arrange
            // var accessToken = await GetAccessToken(tokenType);
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = httpMethod,
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/userinfo",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // #if DEBUG
            //             WriteJsonToFile(@"c:\cdr\userinfo_actual.json", await response.Content.ReadAsStringAsync());
            // #endif

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<AC01_AC02_Expected>(actualJson);

                actual.Should().NotBeNull();
                actual?.name.Should().Be(expectedName);
                actual?.given_name.Should().Be(expectedGivenName);
                actual?.family_name.Should().Be(expectedFamilyName);
                actual?.iss.Should().Be("https://localhost:8001");
                actual?.aud.Should().Be(SOFTWAREPRODUCT_ID.ToLower());

                // TODO - check sub
                //// var sub = IdPermanenceEncrypt(login, customerId, SOFTWAREPRODUCT_ID);
                // var sub = IdPermanenceEncrypt("bis2", CUSTOMERID_BUSINESS2, SOFTWAREPRODUCT_ID);
                // actual?.sub.Should().Be(sub);   
            }
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, "Jane Wilson", "Jane", "Wilson")]
        [InlineData(TokenType.BEVERAGE, "Beverage", "Bob", "Dylan")]
        public async Task AC01_Get_ShouldRespondWith_200OK_UserInfo(TokenType tokenType, string expectedName, string expectedGivenName, string expectedFamilyName)
        {
            await Test_AC01_AC02(HttpMethod.Get, tokenType, expectedName, expectedGivenName, expectedFamilyName);
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, "Jane Wilson", "Jane", "Wilson")]
        [InlineData(TokenType.BEVERAGE, "Beverage", "Bob", "Dylan")]
        public async Task AC02_Post_ShouldRespondWith_200OK_UserInfo(TokenType tokenType, string expectedName, string expectedGivenName, string expectedFamilyName)
        {
            await Test_AC01_AC02(HttpMethod.Post, tokenType, expectedName, expectedGivenName, expectedFamilyName);
        }

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        [InlineData("SUSPENDED", "Suspended", HttpStatusCode.Forbidden)]
        [InlineData("REVOKED", "Revoked", HttpStatusCode.Forbidden)]
        [InlineData("SURRENDERED", "Surrendered", HttpStatusCode.Forbidden)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        public async Task AC03_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_ErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            var saveStatus = GetStatus(Table.LEGALENTITY, LEGALENTITYID);
            SetStatus(Table.LEGALENTITY, LEGALENTITYID, status);
            try
            {
                // Arrange
                // var accessToken = await GetAccessToken(TokenType.JANE_WILSON);
                var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);                

                // Act
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/connect/userinfo",
                    XV = "1",
                    XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                    AccessToken = accessToken
                };
                var response = await api.SendAsync();

                // Assert
                using (new AssertionScope())
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedStatusCode);

                    // Assert - Check error response
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var expectedContent = $@"
                        {{
                            ""errors"": [
                                {{
                                ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                                ""title"": ""ADR Status Is Not Active"",
                                ""detail"": ""ADR status is {statusDescription}"",
                                ""meta"": {{}}
                                }}
                            ]
                        }}";
                        await Assert_HasContent_Json(expectedContent, response.Content);
                    }
                }
            }
            finally
            {
                SetStatus(Table.LEGALENTITY, LEGALENTITYID, saveStatus);
            }
        }

        /* AC04 was removed from the US
        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        public async Task AC04_Get_WithADRBrandNotActive_ShouldRespondWith_403Forbidden_ErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            var saveStatus = GetStatus(Table.BRAND, BRANDID);
            SetStatus(Table.BRAND, BRANDID, status);
            try
            {
                // Arrange
                // var accessToken = await GetAccessToken(TokenType.JANE_WILSON);
                var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

                // Act
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/connect/userinfo",
                    XV = "1",
                    XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                    AccessToken = accessToken
                };
                var response = await api.SendAsync();

                // Assert
                using (new AssertionScope())
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedStatusCode);

                    // Assert - Check error response
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var expectedContent = $@"
                        {{
                            ""errors"": [
                                {{
                                ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                                ""title"": ""ADR Status Is Not Active"",
                                ""detail"": ""ADR status is {statusDescription}"",
                                ""meta"": {{}}
                                }}
                            ]
                        }}";
                        await Assert_HasContent_Json(expectedContent, response.Content);
                    }
                }
            }
            finally
            {
                SetStatus(Table.BRAND, BRANDID, saveStatus);
            }
        }
        */

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        public async Task AC05_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_ErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON); // Ensure token cache is populated before changing status in case InlineData scenarios above are run/debugged out of order

            var saveStatus = GetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID);
            SetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID, status);
            try
            {
                // Arrange
                // var accessToken = await GetAccessToken(TokenType.JANE_WILSON);
                var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

                // Act
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/connect/userinfo",
                    XV = "1",
                    XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                    AccessToken = accessToken
                };
                var response = await api.SendAsync();

                // Assert
                using (new AssertionScope())
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedStatusCode);

                    // Assert - Check error response
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var expectedContent = $@"
                        {{
                            ""errors"": [
                                {{
                                ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                                ""title"": ""ADR Status Is Not Active"",
                                ""detail"": ""Software product status is {statusDescription}"",
                                ""meta"": {{}}
                                }}
                            ]
                        }}";
                        await Assert_HasContent_Json(expectedContent, response.Content);
                    }
                }
            }
            finally
            {
                SetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID, saveStatus);
            }
        }

        private async Task Test_AC06_AC07(TokenType tokenType, HttpStatusCode expectedStatusCode, string expectedWWWAuthenticateResponse)
        {
            // Arrange
            // var accessToken = await GetAccessToken(tokenType);
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/userinfo",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await Assert_HasNoContent2(response.Content);

                    // Not in AC - commenting out
                    // Assert - Check error response                     
                    // Assert_HasHeader(expectedWWWAuthenticateResponse, response.Headers, "WWW-Authenticate");  
                }
            }
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_EMPTY, HttpStatusCode.Unauthorized)]
        [InlineData(TokenType.INVALID_OMIT, HttpStatusCode.Unauthorized)]
        public async Task AC06_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorised_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            await Test_AC06_AC07(tokenType, expectedStatusCode, "Bearer");
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_FOO, HttpStatusCode.Unauthorized)]
        public async Task AC07_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorised_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            await Test_AC06_AC07(tokenType, expectedStatusCode, @"Bearer error=""invalid_token""");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task AC08_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorised_WWWAuthenticateHeader(HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = BaseTest.EXPIRED_CONSUMER_ACCESS_TOKEN;

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/userinfo",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                { 
                    // Assert - Check error response 
                    Assert_HasHeader(@"Bearer realm=""IdentityServer"",error=""invalid_token"",error_description=""The access token expired""",
                        response.Headers,
                        "WWW-Authenticate");
                }
            }
        }

        [Theory]
        [InlineData(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, HttpStatusCode.OK)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.Unauthorized)]  // Different holder of key
        public async Task AC09_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorised(string certificateFilename, string certificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // var accessToken = await GetAccessToken(TokenType.JANE_WILSON);
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = certificateFilename,
                CertificatePassword = certificatePassword,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/userinfo",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await Assert_HasNoContent2(response.Content);
                }
            }
        }
    }
}
