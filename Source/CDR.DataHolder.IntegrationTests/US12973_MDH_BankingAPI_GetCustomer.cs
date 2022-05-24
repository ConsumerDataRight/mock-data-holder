using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Newtonsoft.Json;
using CDR.DataHolder.Repository.Infrastructure;
using CDR.DataHolder.IntegrationTests.Extensions;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.IntegrationTests.Fixtures;

namespace CDR.DataHolder.IntegrationTests
{
    public class US12973_MDH_BankingAPI_GetCustomer : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private RegisterSoftwareProductFixture Fixture { get; init; }

        public US12973_MDH_BankingAPI_GetCustomer(RegisterSoftwareProductFixture fixture)
        {
            Fixture = fixture;
        }

        private const string SCOPE_WITHOUT_CUSTOMERBASICREAD = "openid bank:accounts.basic:read bank:transactions:read";

        private static string GetExpectedResponse(string accessToken)
        {
            // Get clientCustomerId from JWT ("sub" claim)
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var sub = jwt.Claim("sub").Value;

            // Decrypt sub to extraxt customer id
            var customerId = new Guid(IdPermanenceHelper.DecryptSub(
                sub,
                new SubPermanenceParameters
                {
                    SoftwareProductId = SOFTWAREPRODUCT_ID.ToLower(),
                    SectorIdentifierUri = SOFTWAREPRODUCT_SECTOR_IDENTIFIER_URI,
                },
                BaseTest.IDPERMANENCE_PRIVATEKEY
            ));

            // Get expected response 
            using var dbContext = new DataHolderDatabaseContext(new DbContextOptionsBuilder<DataHolderDatabaseContext>().UseSqlServer(DATAHOLDER_CONNECTIONSTRING).Options);
            var expectedResponse = new
            {
                data = dbContext.Customers.AsNoTracking()
                    .Include(person => person.Person)
                    .Include(organisaton => organisaton.Organisation)
                    .Where(customer => customer.CustomerId == customerId)
                    .Select(customer => new
                    {
                        customerUType = customer.CustomerUType,
                        person = customer.CustomerUType.ToLower() == "person" ? new
                        {
                            lastUpdateTime = customer.Person.LastUpdateTime,
                            firstName = customer.Person.FirstName,
                            lastName = customer.Person.LastName,
                            middleNames = string.IsNullOrEmpty(customer.Person.MiddleNames) ?
                                null :
                                customer.Person.MiddleNames.Split(',', System.StringSplitOptions.TrimEntries),
                            prefix = customer.Person.Prefix,
                            suffix = customer.Person.Suffix,
                            occupationCode = customer.Person.OccupationCode,
                            occupationCodeVersion = customer.Person.OccupationCodeVersion,
                        } : null,
                        organisation = customer.CustomerUType.ToLower() == "organisation" ? new
                        {
                            lastUpdateTime = customer.Organisation.LastUpdateTime,
                            agentFirstName = customer.Organisation.AgentFirstName,
                            agentLastName = customer.Organisation.AgentLastName,
                            agentRole = customer.Organisation.AgentRole,
                            businessName = customer.Organisation.BusinessName,
                            legalName = customer.Organisation.LegalName,
                            shortName = customer.Organisation.ShortName,
                            abn = customer.Organisation.Abn,
                            acn = customer.Organisation.Acn,
                            isACNCRegistered = customer.Organisation.IsAcnCRegistered,
                            industryCode = customer.Organisation.IndustryCode,
                            industryCodeVersion = customer.Organisation.IndustryCodeVersion,
                            organisationType = customer.Organisation.OrganisationType,
                            registeredCountry = customer.Organisation.RegisteredCountry,
                            establishmentDate = customer.Organisation.EstablishmentDate == null ?
                                 null :
                                 customer.Organisation.EstablishmentDate.Value.ToString("yyyy-MM-dd")
                        } : null,
                    })
                    .FirstOrDefault(),
                links = new
                {
                    self = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer"
                },
                meta = new { }
            };

            return JsonConvert.SerializeObject(expectedResponse,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                });
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON)]
        [InlineData(TokenType.BEVERAGE)]
        public async Task AC01_ShouldRespondWith_200OK_Customers(TokenType tokenType)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType);

            var expectedResponse = GetExpectedResponse(accessToken);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check XV
                Assert_HasHeader(api.XV, response.Headers, "x-v");

                // Assert - Check x-fapi-interaction-id
                Assert_HasHeader(null, response.Headers, "x-fapi-interaction-id");

                // Assert - Check json
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Theory]
        [InlineData(SCOPE, HttpStatusCode.OK)]
        [InlineData(SCOPE_WITHOUT_CUSTOMERBASICREAD, HttpStatusCode.Forbidden)]
        public async Task AC02_Get_WithoutScopeBasicRead_ShouldRespondWith_403Forbidden(string scope, HttpStatusCode expectedStatusCode)
        {
            // Arrange 
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON, scope);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
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

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var expectedResponse = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidConsent"",
                            ""title"": ""Consent Is Invalid"",
                            ""detail"": ""The authorised consumer's consent is insufficient to execute the resource"",
                            ""meta"": {}
                        }]
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        private async Task Test_AC03_AC03b_AC03c(string XV, HttpStatusCode expectedStatusCode, string expectedErrorResponse)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
                XV = XV,
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
                    // Assert - Check content type 
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Assert - Check error response
                    await Assert_HasContent_Json(expectedErrorResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData("1", HttpStatusCode.OK)]
        [InlineData("2", HttpStatusCode.NotAcceptable)]
        public async Task AC03_Get_WithXV2_ShouldRespondWith_406NotAcceptable(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test_AC03_AC03b_AC03c(XV, expectedStatusCode, @"
                {
                    ""errors"": [
                        {
                        ""code"": ""urn:au-cds:error:cds-all:Header/UnsupportedVersion"",
                        ""title"": ""Unsupported Version"",
                        ""detail"": ""The minimum supported version is 1. The maximum supported version is 1."",
                        ""meta"": {}
                        }
                    ]
                }");
        }

        [Theory]
        [InlineData("1", HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData("99999999999999999999999999999999999999999999999999", HttpStatusCode.BadRequest)]
        [InlineData("-1", HttpStatusCode.BadRequest)]
        public async Task AC03b_Get_WithInvalidXV_ShouldRespondWith_400BadRequest(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test_AC03_AC03b_AC03c(XV, expectedStatusCode, @"
                {
                    ""errors"": [
                        {
                        ""code"": ""urn:au-cds:error:cds-all:Header/InvalidVersion"",
                        ""title"": ""Invalid Version"",
                        ""detail"": ""Version header must be a positive integer"",
                        ""meta"": {}
                        }
                    ]
                }");
        }

        [Theory]
        [InlineData("1", HttpStatusCode.OK)]
        [InlineData("", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC03c_Get_WithMissingXV_ShouldRespondWith_400BadRequest(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test_AC03_AC03b_AC03c(XV, expectedStatusCode, @"
                {
                    ""errors"": [
                        {
                        ""code"": ""urn:au-cds:error:cds-all:Header/Missing"",
                        ""title"": ""Missing Required Header"",
                        ""detail"": ""The header 'x-v' is missing."",
                        ""meta"": {}
                        }
                    ]
                }");
        }

        [Theory]
        [InlineData("DateTime.Now.RFC1123", HttpStatusCode.OK)]
        [InlineData(null, HttpStatusCode.BadRequest)]  // omit xfapiauthdate
        public async Task AC04_Get_WithMissingXFAPIAUTHDATE_ShouldRespondWith_400BadRequest_HeaderMissingErrorResponse(string XFapiAuthDate, HttpStatusCode expectedHttpStatusCode)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

            XFapiAuthDate = GetDate(XFapiAuthDate);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
                XV = "1",
                XFapiAuthDate = XFapiAuthDate,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedHttpStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check content type 
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Assert - Check error response
                    var expectedContent = @"
                    {
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Header/Missing"",
                            ""title"": ""Missing Required Header"",
                            ""detail"": ""The header 'x-fapi-auth-date' is missing."",
                            ""meta"": {}
                        }]
                    }";
                    await Assert_HasContent_Json(expectedContent, response.Content);
                }
            }
        }

        [Theory]
        [InlineData("DateTime.Now.RFC1123", HttpStatusCode.OK)]
        [InlineData("DateTime.UtcNow", HttpStatusCode.BadRequest)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC05_Get_WithInvalidXFAPIAUTHDATE_ShouldRespondWith_400BadRequest_HeaderInvalidErrorResponse(string XFapiAuthDate, HttpStatusCode expectedHttpStatusCode)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);
            XFapiAuthDate = GetDate(XFapiAuthDate);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
                XV = "1",
                XFapiAuthDate = XFapiAuthDate,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedHttpStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check content type 
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Assert - Check error response
                    var expectedContent = @"
                    {
                        ""errors"": [
                        {
                            ""code"": ""urn:au-cds:error:cds-all:Header/Invalid"",
                            ""title"": ""Invalid Header"",
                            ""detail"": ""The header 'x-fapi-auth-date' is invalid."",
                            ""meta"": {}
                        }]
                    }";
                    await Assert_HasContent_Json(expectedContent, response.Content);
                }
            }
        }

        private async Task Test_AC06_AC07(TokenType tokenType, HttpStatusCode expectedStatusCode, string expectedWWWAuthenticateResponse)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
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
                    Assert_HasHeader(expectedWWWAuthenticateResponse, response.Headers, "WWW-Authenticate");
                }
            }
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_EMPTY, HttpStatusCode.Unauthorized)]
        [InlineData(TokenType.INVALID_OMIT, HttpStatusCode.Unauthorized)]
        public async Task AC06_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            await Test_AC06_AC07(tokenType, expectedStatusCode, "Bearer");
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_FOO, HttpStatusCode.Unauthorized)]
        public async Task AC07_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            await Test_AC06_AC07(tokenType, expectedStatusCode, @"Bearer error=""invalid_token""");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task AC08_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = BaseTest.EXPIRED_CONSUMER_ACCESS_TOKEN; // await CreateAccessToken_UsingAuthCode(TokenType.JANE_WILSON, expired: expired);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
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
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        response.Headers,
                        "WWW-Authenticate",
                        true); // starts with
                }
            }
        }

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        [InlineData("SUSPENDED", "Suspended", HttpStatusCode.Forbidden)]
        [InlineData("REVOKED", "Revoked", HttpStatusCode.Forbidden)]
        [InlineData("SURRENDERED", "Surrendered", HttpStatusCode.Forbidden)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        public async Task AC09_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            var saveStatus = GetStatus(Table.LEGALENTITY, LEGALENTITYID);
            SetStatus(Table.LEGALENTITY, LEGALENTITYID, status);
            try
            {
                // Arrange
                var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

                // Act
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
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
                        var expectedContent = $@"{{
                            ""errors"": [{{
                                ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                                ""title"": ""ADR Status Is Not Active"",
                                ""detail"": ""ADR status is { statusDescription }"",
                                ""meta"": {{}}
                            }}]
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

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        public async Task AC11_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON); // Ensure token cache is populated before changing status in case InlineData scenarios above are run/debugged out of order

            var saveStatus = GetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID);
            SetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID, status);
            try
            {
                // Arrange
                var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

                // Act
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
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
                        var expectedContent = $@"{{
                            ""errors"": [{{
                                ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                                ""title"": ""ADR Status Is Not Active"",
                                ""detail"": ""Software product status is { statusDescription }"",
                                ""meta"": {{}}
                            }}]
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

        [Theory]
        [InlineData(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, HttpStatusCode.OK)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.Unauthorized)]  // Different holder of key
        public async Task AC12_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorized(string certificateFilename, string certificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = certificateFilename,
                CertificatePassword = certificatePassword,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/common/customer",
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
                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    var expectedResponse = @"{
                        ""errors"": [
                            {
                                ""code"": ""401"",
                                ""title"": ""Unauthorized"",
                                ""detail"": ""invalid_token"",
                                ""meta"": {}
                            }
                        ]
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }
    }
}
