using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;
using CDR.DataHolder.IntegrationTests.Infrastructure;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using CDR.DataHolder.IntegrationTests.Models.BankingAccountsResponse;
using CDR.DataHolder.Repository.Infrastructure;
using CDR.DataHolder.IntegrationTests.Fixtures;
using CDR.DataHolder.IntegrationTests.Models;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US12975_MDH_BankingAPI_GetAccounts : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private RegisterSoftwareProductFixture Fixture { get; init; }

        public US12975_MDH_BankingAPI_GetAccounts(RegisterSoftwareProductFixture fixture)
        {
            Fixture = fixture;
        }

        protected const string SCOPE_WITHOUT_ACCOUNTSBASICREAD = "openid common:customer.basic:read bank:transactions:read";

        private static (string, int) GetExpectedResponse(string? accessToken,
            string baseUrl, string selfUrl,
            bool? isOwned = null, string? openStatus = null, string? productCategory = null,
            int? page = null, int? pageSize = null)
        {
            ExtractClaimsFromToken(accessToken, out var loginId, out var softwareProductId);

            var effectivePage = page ?? 1;
            var effectivePageSize = pageSize ?? 25;

            using var dbContext = new DataHolderDatabaseContext(new DbContextOptionsBuilder<DataHolderDatabaseContext>().UseSqlServer(DATAHOLDER_CONNECTIONSTRING).Options);

            // NB: This has to compare decrypted Id's as AES Encryption now uses a Random IV,
            //     using encrypted ID's in the response and expected content WILL NEVER MATCH
            var accounts = dbContext.Accounts.AsNoTracking()
                .Include(account => account.Customer)
                // .Where(account => account.Customer.CustomerId == new Guid(customerId))
                .Where(account => account.Customer.LoginId == loginId)
                .Select(account => new
                {
                    accountId = IdPermanenceEncrypt(account.AccountId, loginId, softwareProductId),
                    creationDate = account.CreationDate.HasValue ? account.CreationDate.Value.ToString("yyyy-MM-dd") : null,
                    displayName = account.DisplayName,
                    nickname = account.NickName,
                    openStatus = account.OpenStatus,
                    isOwned = true,
                    maskedNumber = account.MaskedName,
                    productCategory = account.ProductCategory,
                    productName = account.ProductName,
                })
                .ToList();

            // Filter
            accounts = accounts
                .Where(account => isOwned == null || account.isOwned == isOwned)
                .Where(account => openStatus == null || account.openStatus == openStatus)
                .Where(account => productCategory == null || account.productCategory == productCategory)
                .ToList();

            var totalRecords = accounts.Count;

            // Paging
            accounts = accounts
                .OrderBy(account => account.displayName).ThenBy(account => account.accountId)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);

            var expectedResponse = new
            {
                data = new
                {
                    accounts,
                },
                links = new
                {
                    first = totalPages == 0 ? null : GetUrl(baseUrl, isOwned, openStatus, productCategory, 1, effectivePageSize),
                    last = totalPages == 0 ? null : GetUrl(baseUrl, isOwned, openStatus, productCategory, totalPages, effectivePageSize),
                    next = totalPages == 0 || effectivePage == totalPages ? null : GetUrl(baseUrl, isOwned, openStatus, productCategory, effectivePage + 1, effectivePageSize),
                    prev = totalPages == 0 || effectivePage == 1 ? null : GetUrl(baseUrl, isOwned, openStatus, productCategory, effectivePage - 1, effectivePageSize),
                    self = selfUrl,
                },
                meta = new
                {
                    totalRecords,
                    totalPages
                }
            };

            return (
                JsonConvert.SerializeObject(expectedResponse, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                }),

                totalRecords
            );
        }

        static string GetUrl(string baseUrl,
            bool? isOwned = null, string? openStatus = null, string? productCategory = null,
            int? queryPage = null, int? queryPageSize = null)
        {
            var query = new KeyValuePairBuilder();

            if (isOwned != null)
            {
                query.Add("is-owned", isOwned.Value ? "true" : "false");
            }

            if (openStatus != null)
            {
                query.Add("open-status", openStatus);
            }

            if (productCategory != null)
            {
                query.Add("product-category", productCategory);
            }

            if (queryPage != null)
            {
                query.Add("page", queryPage.Value);
            }

            if (queryPageSize != null)
            {
                query.Add("page-size", queryPageSize.Value);
            }

            return query.Count > 0 ?
                $"{baseUrl}?{query.Value}" :
                baseUrl;
        }

        private async Task Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(
            TokenType tokenType,
            bool? isOwned = null, string? openStatus = null, string? productCategory = null,
            int? queryPage = null, int? queryPageSize = null,
            int? expectedRecordCount = null)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType);

            var baseUrl = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts";
            var url = GetUrl(baseUrl, isOwned, openStatus, productCategory, queryPage, queryPageSize);

            (var expectedResponse, var totalRecords) = GetExpectedResponse(accessToken, baseUrl, url,
                isOwned, openStatus, productCategory,
                queryPage, queryPageSize);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = url,
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

                // Assert - Record count
                if (expectedRecordCount != null)
                {
                    totalRecords.Should().Be(expectedRecordCount);
                }
            }
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON)]
        [InlineData(TokenType.KAMILLA_SMITH)]
        [InlineData(TokenType.BEVERAGE, Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51320")]
        public async Task AC01_Get_ShouldRespondWith_200OK_Accounts(TokenType tokenType)
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType);
        }

        [Fact]
        public async Task AC02_Get_WithPageSize5_ShouldRespondWith_200OK_Page1Of5Records()
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(TokenType.KAMILLA_SMITH, queryPage: 1, queryPageSize: 5);
        }

        [Fact]
        public async Task AC03_Get_WithPageSize5_AndPage3_ShouldRespondWith_200OK_Page3Of5Records()
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(TokenType.KAMILLA_SMITH, queryPage: 3, queryPageSize: 5);
        }

        [Fact]
        public async Task AC04_Get_WithPageSize5_AndPage5_ShouldRespondWith_200OK_Page5Of5Records()
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(TokenType.KAMILLA_SMITH, queryPage: 5, queryPageSize: 5);
        }

        [Theory]
        [InlineData(TokenType.KAMILLA_SMITH, 0)]
        public async Task AC05_Get_WithIsOwnedFalse_ShouldRespondWith_200OK_NonOwnedAccounts(TokenType tokenType, int expectedRecordCount)
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType, isOwned: false, expectedRecordCount: expectedRecordCount, queryPage: 1, queryPageSize: 5);
        }

        [Theory]
        [InlineData(TokenType.KAMILLA_SMITH, 9)]
        public async Task AC06_Get_WithOpenStatusCLOSED_ShouldRespondWith_200OK_ClosedAccounts(TokenType tokenType, int expectedRecordCount)
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType, openStatus: "CLOSED", expectedRecordCount: expectedRecordCount, queryPage: 1, queryPageSize: 5);
        }

        [Theory]
        [InlineData(TokenType.KAMILLA_SMITH, "OPEN", "PERS_LOANS", 7)]
        [InlineData(TokenType.KAMILLA_SMITH, "OPEN", "BUSINESS_LOANS", 0)]
        [InlineData(TokenType.BUSINESS_1, "OPEN", "BUSINESS_LOANS", 1, Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51320")]
        [InlineData(TokenType.BEVERAGE, "CLOSED", "BUSINESS_LOANS", 1, Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51320")]
        public async Task AC07_Get_WithOpenStatusOPEN_AndProductCategoryBUSINESSLOANS_ShouldRespondWith_200OK_OpenedBusinessLoanAccounts(TokenType tokenType, string openStatus, string productCategory, int expectedRecordCount)
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType, openStatus: openStatus, productCategory: productCategory, expectedRecordCount: expectedRecordCount, queryPage: 1, queryPageSize: 5);
        }

        [Theory]
        [InlineData(SCOPE, HttpStatusCode.OK)]
        [InlineData(SCOPE_WITHOUT_ACCOUNTSBASICREAD, HttpStatusCode.Forbidden)]
        public async Task AC08_Get_WithoutBankAccountsReadScope_ShouldRespondWith_403Forbidden(string scope, HttpStatusCode expectedStatusCode)
        {
            // Arrange 
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.KAMILLA_SMITH, scope);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
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

        private async Task Test_AC09_AC11(TokenType tokenType, HttpStatusCode expectedStatusCode, string expectedWWWAuthenticateResponse)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
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
        [InlineData(TokenType.KAMILLA_SMITH, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_FOO, HttpStatusCode.Unauthorized)]
        public async Task AC09_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorized(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
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

        [Theory]
        [InlineData(TokenType.KAMILLA_SMITH, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_EMPTY, HttpStatusCode.Unauthorized)]
        [InlineData(TokenType.INVALID_OMIT, HttpStatusCode.Unauthorized)]
        public async Task AC11_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorized(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            await Test_AC09_AC11(tokenType, expectedStatusCode, "Bearer");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task AC10_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized(HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = BaseTest.EXPIRED_CONSUMER_ACCESS_TOKEN; 

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
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

        private async Task Test_AC12_AC13_AC14(EntityType entityType, string id, string status, HttpStatusCode expectedStatusCode, string? expectedErrorResponse = null)
        {
            var saveStatus = GetStatus(entityType, id);
            SetStatus(entityType, id, status);

            try
            {
                var accessToken = string.Empty;
                // Arrange
                try
                {
                    accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.KAMILLA_SMITH); // Ensure token cache is populated before changing status in case InlineData scenarios above are run/debugged out of order
                }
                catch (AuthoriseException ex)
                {
                    // Assert
                    using (new AssertionScope())
                    {

                        // Assert - Check error response
                        ex.Error.Should().Be("urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive");
                        ex.ErrorDescription.Should().Be(expectedErrorResponse);

                        return;
                    }
                }

                // Act
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
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
                }
            }
            finally
            {
                SetStatus(entityType, id, saveStatus);
            }
        }

        [Theory]
        [InlineData("ACTIVE", HttpStatusCode.OK)]
        [InlineData("INACTIVE",  HttpStatusCode.BadRequest)]
        [InlineData("REMOVED", HttpStatusCode.BadRequest)]
        public async Task AC12_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(string status, HttpStatusCode expectedStatusCode)
        {
            await Test_AC12_AC13_AC14(EntityType.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID, status, expectedStatusCode, "Software product status is { statusDescription }");
        }

        [Theory]
        [InlineData("ACTIVE", HttpStatusCode.OK)]
        [InlineData("INACTIVE", HttpStatusCode.BadRequest)]
        [InlineData("REMOVED", HttpStatusCode.BadRequest)]
        public async Task AC14_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(string status, HttpStatusCode expectedStatusCode)
        {
            await Test_AC12_AC13_AC14(EntityType.LEGALENTITY, LEGALENTITYID, status, expectedStatusCode, "ADR status is { statusDescription }");
        }

        private async Task Test_AC15_AC16_AC17(string XV, HttpStatusCode expectedStatusCode, string expectedErrorResponse)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.KAMILLA_SMITH);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
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
        public async Task AC15_Get_WithXV2_ShouldRespondWith_406NotAcceptable(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test_AC15_AC16_AC17(XV, expectedStatusCode, @"
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
        public async Task AC16_Get_WithInvalidXV_ShouldRespondWith_400BadRequest(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test_AC15_AC16_AC17(XV, expectedStatusCode, @"
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
        public async Task AC17_Get_WithMissingXV_ShouldRespondWith_400BadRequest(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test_AC15_AC16_AC17(XV, expectedStatusCode, @"
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

        private async Task Test_AC18_AC19(string? XFapiAuthDate, HttpStatusCode expectedStatusCode, string? expectedErrorResponse = null)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.KAMILLA_SMITH);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                XV = "1",
                XFapiAuthDate = XFapiAuthDate,
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
        [InlineData("DateTime.Now.RFC1123", HttpStatusCode.OK)]
        [InlineData(null, HttpStatusCode.BadRequest)]  // omit xfapiauthdate
        public async Task AC18_Get_WithMissingXFAPIAUTHDATE_ShouldRespondWith_400BadRequest(string XFapiAuthDate, HttpStatusCode expectedStatusCode)
        {
            await Test_AC18_AC19(GetDate(XFapiAuthDate), expectedStatusCode, @"
                {
                    ""errors"": [
                        {
                        ""code"": ""urn:au-cds:error:cds-all:Header/Missing"",
                        ""title"": ""Missing Required Header"",
                        ""detail"": ""The header 'x-fapi-auth-date' is missing."",
                        ""meta"": {}
                        }
                    ]
                }");
        }

        [Theory]
        [InlineData("DateTime.Now.RFC1123", HttpStatusCode.OK)]
        [InlineData("DateTime.UtcNow", HttpStatusCode.BadRequest)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async void AC19_Get_WithInvalidXFAPIAUTHDATE_ShouldRespondWith_400BadRequest(string XFapiAuthDate, HttpStatusCode expectedStatusCode)
        {
            await Test_AC18_AC19(GetDate(XFapiAuthDate), expectedStatusCode, @"
                {
                    ""errors"": [
                        {
                        ""code"": ""urn:au-cds:error:cds-all:Header/Invalid"",
                        ""title"": ""Invalid Header"",
                        ""detail"": ""The header 'x-fapi-auth-date' is invalid."",
                        ""meta"": {}
                        }
                    ]
                }");
        }

        [Theory]
        [InlineData("123", HttpStatusCode.OK)]
        public async Task AC20_Get_WithXFAPIInteractionId123_ShouldRespondWith_200OK_Accounts_AndXFapiInteractionIDis123(string xFapiInteractionId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.KAMILLA_SMITH);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                XFapiInteractionId = xFapiInteractionId,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check x-fapi-interaction-id header
                Assert_HasHeader(xFapiInteractionId, response.Headers, "x-fapi-interaction-id");
            }
        }

        [Theory]
        [InlineData(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, HttpStatusCode.OK)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.Unauthorized)]  // Different holder of key
        public async Task AC21_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorized(string certificateFilename, string certificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.KAMILLA_SMITH);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = certificateFilename,
                CertificatePassword = certificatePassword,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
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

        [Theory]
        [InlineData(USERID_JANEWILSON, "98765988,98765987")] // All accounts
        [InlineData(USERID_JANEWILSON, "98765988")] // Subset of accounts
        public async Task ACX01_Get_WhenConsumerDidNotGrantConsentToAllAccounts_ShouldRespondWith_200OK_ConsentedAccounts(string userId, string consentedAccounts)
        {
            static async Task<Response?> GetAccounts(string? accessToken)
            {
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                    XV = "1",
                    XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                    AccessToken = accessToken
                };
                var response = await api.SendAsync();
                if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Error getting accounts");

                var json = await response.Content.ReadAsStringAsync();

                var accountsResponse = JsonConvert.DeserializeObject<Response>(json);

                return accountsResponse;
            }

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = userId,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = consentedAccounts,
                RequestUri = await PAR_GetRequestUri(responseMode: "form_post")
            }.Authorise();

            // Act
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            var accountsResponse = await GetAccounts(tokenResponse?.AccessToken);

            ExtractClaimsFromToken(tokenResponse?.AccessToken, out var loginId, out var softwareProductId);
            var encryptedAccountIds = consentedAccounts.Split(',').Select(consentedAccountId => IdPermanenceEncrypt(consentedAccountId, loginId, softwareProductId));

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check each account in response is one of the consented accounts
                foreach (var account in accountsResponse?.Data?.Accounts ?? throw new NullReferenceException())
                {
                    encryptedAccountIds.Should().Contain(account.AccountId);
                }
            }
        }

        [Theory]
        [InlineData(USERID_JANEWILSON, "98765988,98765987")] // All accounts
        public async Task ACX02_GetAccountsMultipleTimes_ShouldRespondWith_SameEncryptedAccountIds(string userId, string consentedAccounts)
        {
            static async Task<string?[]?> GetAccountIds(string userId, string consentedAccounts)
            {
                static async Task<Response?> GetAccounts(string? accessToken)
                {
                    var api = new Infrastructure.API
                    {
                        CertificateFilename = CERTIFICATE_FILENAME,
                        CertificatePassword = CERTIFICATE_PASSWORD,
                        HttpMethod = HttpMethod.Get,
                        URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                        XV = "1",
                        XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                        AccessToken = accessToken
                    };
                    var response = await api.SendAsync();
                    if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Error getting accounts");

                    var json = await response.Content.ReadAsStringAsync();

                    var accountsResponse = JsonConvert.DeserializeObject<Response>(json);

                    return accountsResponse;
                }

                // Get authcode
                (var authCode, _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = userId,
                    OTP = AUTHORISE_OTP,
                    SelectedAccountIds = consentedAccounts,
                    RequestUri = await PAR_GetRequestUri(responseMode: "form_post")
                }.Authorise();

                // Get token
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

                // Get accounts
                var accountsResponse = await GetAccounts(tokenResponse?.AccessToken);

                // Return list of account ids
                return accountsResponse?.Data?.Accounts?.Select(x => x.AccountId).ToArray();
            }

            // Act - Get accounts
            var encryptedAccountIDs1 = await GetAccountIds(userId, consentedAccounts);

            // Act - Get accounts again
            var encryptedAccountIDs2 = await GetAccountIds(userId, consentedAccounts);

            // Assert
            using (new AssertionScope())
            {
                encryptedAccountIDs1.Should().NotBeNullOrEmpty();
                encryptedAccountIDs2.Should().NotBeNullOrEmpty();

                // Assert - Encrypted account ids should be same
                encryptedAccountIDs1.Should().BeEquivalentTo(encryptedAccountIDs2);
            }
        }
    }
}
