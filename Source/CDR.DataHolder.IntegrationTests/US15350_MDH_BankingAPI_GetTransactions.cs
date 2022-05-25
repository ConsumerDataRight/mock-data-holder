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
using CDR.DataHolder.Repository.Infrastructure;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US15350_MDH_BankingAPI_GetTransactions : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private RegisterSoftwareProductFixture Fixture { get; init; }

        public US15350_MDH_BankingAPI_GetTransactions(RegisterSoftwareProductFixture fixture)
        {
            Fixture = fixture;
        }

        private const string SCOPE_WITHOUT_TRANSACTIONSREAD = "openid common:customer.basic:read bank:accounts.basic:read";

        private const string FOO_GUID = "F0000000-F000-F000-F000-F00000000000";

        // Note: These default dates are based on the current seed-data.json file to select a valid data set.
        private static string DEFAULT_EFFECTIVENEWESTTIME => "2021-06-01T00:00:00Z";
        private static string DEFAULT_EFFECTIVEOLDESTTIME => "2021-03-01T00:00:00Z";

        private static (string, int) GetExpectedResponse(
            string accountId, string? accessToken,
            string baseUrl, string selfUrl,
            string? oldestTime = null, string? newestTime = null, string? minAmount = null, string? maxAmount = null, string? text = null,
            int? page = null, int? pageSize = null)
        {
            ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);

            var effectivePage = page ?? 1;
            var effectivePageSize = pageSize ?? 25;

            var effectiveNewestTime = newestTime;
            if (effectiveNewestTime == null) { effectiveNewestTime = DEFAULT_EFFECTIVENEWESTTIME; }

            var effectiveOldestTime = oldestTime;
            if (effectiveOldestTime == null) { effectiveOldestTime = DEFAULT_EFFECTIVEOLDESTTIME; }

            using var dbContext = new DataHolderDatabaseContext(new DbContextOptionsBuilder<DataHolderDatabaseContext>().UseSqlServer(DATAHOLDER_CONNECTIONSTRING).Options);

            // NB: This has to compare decrypted Id's as AES Encryption now uses a Random IV,
            //     using encrypted ID's in the response and expected content WILL NEVER MATCH
            var transactions = dbContext.Transactions.AsNoTracking()
                .Include(transaction => transaction.Account)
                .Where(transaction => transaction.AccountId == accountId && transaction.Account.CustomerId == new Guid(customerId))
                .Select(transaction => new
                {
                    accountId = IdPermanenceEncrypt(transaction.AccountId, customerId, softwareProductId),
                    transactionId = IdPermanenceEncrypt(transaction.TransactionId, customerId, softwareProductId),
                    isDetailAvailable = false,
                    type = transaction.TransactionType,
                    status = transaction.Status,
                    description = transaction.Description,
                    postingDateTime = transaction.PostingDateTime,
                    valueDateTime = transaction.ValueDateTime,
                    executionDateTime = transaction.ExecutionDateTime,
                    amount = transaction.Amount.ToString("0.00"),
                    currency = transaction.Currency,
                    reference = transaction.Reference,
                    merchantName = transaction.MerchantName,
                    merchantCategoryCode = transaction.MerchantCategoryCode,
                    billerCode = transaction.BillerCode,
                    billerName = transaction.BillerName,
                    crn = transaction.Crn,
                    apcaNumber = transaction.ApcaNumber,
                })
                .ToList();

            // Filter
            transactions = transactions
                .Where(transaction => (transaction.postingDateTime ?? transaction.executionDateTime) >= DateTime.Parse(effectiveOldestTime).ToUniversalTime())
                .Where(transaction => (transaction.postingDateTime ?? transaction.executionDateTime) <= DateTime.Parse(effectiveNewestTime).ToUniversalTime())
                .Where(transaction => minAmount == null || Decimal.Parse(transaction.amount) >= Decimal.Parse(minAmount))
                .Where(transaction => maxAmount == null || Decimal.Parse(transaction.amount) <= Decimal.Parse(maxAmount))
                .Where(transaction => text == null ||
                    transaction.description.Contains(text, StringComparison.InvariantCultureIgnoreCase) ||
                    transaction.reference.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            var totalRecords = transactions.Count;

            // Paging
            transactions = transactions
                .OrderByDescending(transaction => transaction.postingDateTime).ThenByDescending(transaction => transaction.executionDateTime)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);

            var expectedResponse = new
            {
                data = new
                {
                    transactions,
                },
                links = new
                {
                    first = totalPages == 0 ? null : GetUrl(baseUrl, oldestTime, newestTime, minAmount, maxAmount, text, 1, effectivePageSize, true),
                    last = totalPages == 0 ? null : GetUrl(baseUrl, oldestTime, newestTime, minAmount, maxAmount, text, totalPages, effectivePageSize, true),
                    next = totalPages == 0 || effectivePage == totalPages ? null : GetUrl(baseUrl, oldestTime, newestTime, minAmount, maxAmount, text, effectivePage + 1, effectivePageSize, true),
                    prev = totalPages == 0 || effectivePage == 1 ? null : GetUrl(baseUrl, oldestTime, newestTime, minAmount, maxAmount, text, effectivePage - 1, effectivePageSize, true),
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
            string? oldestTime = null, string? newestTime = null, string? minAmount = null, string? maxAmount = null, string? text = null,
            int? queryPage = null, int? queryPageSize = null,
            bool isLink = false)
        {
            var query = new KeyValuePairBuilder();

            if (oldestTime != null)
            {
                query.Add("oldest-time", isLink ? oldestTime.Replace(":", "%3A") : oldestTime);
            }

            if (newestTime != null)
            {
                query.Add("newest-time", isLink ? newestTime.Replace(":", "%3A") : newestTime);
            }

            if (oldestTime == null && newestTime == null)
            {
                query.Add("oldest-time", isLink ? DEFAULT_EFFECTIVEOLDESTTIME.Replace(":", "%3A") : DEFAULT_EFFECTIVEOLDESTTIME);
                query.Add("newest-time", isLink ? DEFAULT_EFFECTIVENEWESTTIME.Replace(":", "%3A") : DEFAULT_EFFECTIVENEWESTTIME);
            }

            if (minAmount != null)
            {
                query.Add("min-amount", minAmount);
            }

            if (maxAmount != null)
            {
                query.Add("max-amount", maxAmount);
            }

            if (text != null)
            {
                query.Add("text", text);
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

        private async Task Test(
            string accountId,
            TokenType tokenType, string tokenScope = SCOPE, bool tokenExpired = false,

            // Request headers
            string? XV = "1",
            string? XFapiAuthDate = "*NOW*",
            string? XFapiInteractionId = null,

            // Request query strings
            string? oldestTime = null,
            string? newestTime = null,
            string? minAmount = null,
            string? maxAmount = null,
            string? text = null,
            int? queryPage = null,
            int? queryPageSize = null,

            // Expected response
            HttpStatusCode? expectedStatusCode = HttpStatusCode.OK,
            string? expectedContentType = "application/json; charset=utf-8",
            string? expectedXV = null,
            string? expectedXFapiInteractionId = null,
            string? expectedWWWAuthenticate = null,
            bool expectedWWWAuthenticateStartsWith = false,
            string? expectedErrorResponse = null,
            int? expectedRecordCount = null
        )
        {
            var accessToken = tokenExpired ?
                BaseTest.EXPIRED_CONSUMER_ACCESS_TOKEN :
                await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(tokenType, tokenScope);

            string custId = string.Empty;
            string softwareProdId = string.Empty;
            string encryptedAccountId;
            if (accessToken == null || accessToken == "" || accessToken == "foo")
            {
                encryptedAccountId = IdPermanenceEncrypt(accountId, FOO_GUID, FOO_GUID); // we need encrypted account id to make api call
            }
            else
            {
                if (tokenExpired)
                {
                    ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId, false);
                    custId = customerId;
                    softwareProdId = softwareProductId;
                }
                else
                {
                    ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId, true);
                    custId = customerId;
                    softwareProdId = softwareProductId;
                }
                encryptedAccountId = IdPermanenceEncrypt(accountId, custId, softwareProdId);
            }

            // Arrange
            if (XFapiAuthDate == "*NOW*")
            {
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r");
            }

            var baseUrl = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts/{encryptedAccountId}/transactions";
            var url = GetUrl(baseUrl, oldestTime, newestTime, minAmount, maxAmount, text, queryPage, queryPageSize);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = url,
                XV = XV,
                XFapiAuthDate = XFapiAuthDate,
                XFapiInteractionId = XFapiInteractionId,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check ContentType (if response has content)
                if (!string.IsNullOrEmpty(await response.Content.ReadAsStringAsync()))
                {
                    response.Content?.Headers.Should().NotBeNull();
                    response.Content?.Headers?.ContentType.Should().NotBeNull();
                    response.Content?.Headers?.ContentType?.ToString().Should().Be(expectedContentType);
                }

                // Assert - Check WWWAutheticate header
                if (response.StatusCode != HttpStatusCode.OK && expectedWWWAuthenticate != null)
                {
                    Assert_HasHeader(expectedWWWAuthenticate, response.Headers, "WWW-Authenticate", expectedWWWAuthenticateStartsWith);
                }

                if (expectedStatusCode == HttpStatusCode.OK && response.StatusCode == HttpStatusCode.OK)
                {
                    // Assert - Check XV
                    Assert_HasHeader(expectedXV ?? api.XV, response.Headers, "x-v");

                    // Assert - Check x-fapi-interaction-id
                    Assert_HasHeader(expectedXFapiInteractionId, response.Headers, "x-fapi-interaction-id");

                    // Get expected response
                    (var expectedResponse, var totalRecords) = GetExpectedResponse(
                        accountId, accessToken,
                        baseUrl, url,
                        oldestTime, newestTime, minAmount, maxAmount, text,
                        queryPage, queryPageSize);

                    // Assert - Check json
                    await Assert_HasContent_Json(expectedResponse, response.Content);

                    // Assert - Record count
                    if (expectedRecordCount != null)
                    {
                        totalRecords.Should().Be(expectedRecordCount);
                    }
                }

                // Assert - Check error response
                if (expectedStatusCode != HttpStatusCode.OK && expectedErrorResponse != null)
                {
                    await Assert_HasContent_Json(expectedErrorResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_Transactions()
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON);
        }

        [Fact]
        public async Task AC02_Get_WithPageSize5_ShouldRespondWith_200OK_Page1Of5Records()
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, queryPage: 1, queryPageSize: 5);
        }

        [Fact]
        public async Task AC03_Get_WithPageSize5_AndPage3_ShouldRespondWith_200OK_Page3Of5Records()
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, queryPage: 3, queryPageSize: 5);
        }

        [Fact]
        public async Task AC04_Get_WithPageSize5_AndPage5_ShouldRespondWith_200OK_Page5Of5Records()
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, queryPage: 5, queryPageSize: 5);
        }

        [Theory]
        [InlineData("2021-05-25T00:00:00.000Z", 2)]
        [InlineData("2021-05-26T00:00:00.000Z", 0)]
        // NB: If the appsettings.ENV.json > SeedData > OffsetDates = true - then the reference date of 2021-05-01 for the data seeded into the database will be moved to now
        //     SO THE ABOVE TEST DATES MUST ALSO BE MOVED as per the OffsetDates as set in 010 Repository > ...\Repository\Infrastructure\Extensions.cs
        //     else this test will FAIL.
        public async Task AC05_Get_WithOldestTime_ShouldRespondWith_200OK_FilteredRecords(string oldestTime, int expectedRecordCount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, oldestTime: oldestTime, expectedRecordCount: expectedRecordCount);
        }

        [Theory]
        [InlineData("2021-03-01T00:00:00.000Z", 0)]
        [InlineData("2021-03-02T00:00:00.000Z", 2)]
        // NB: If the appsettings.ENV.json > SeedData > OffsetDates = true - then the reference date of 2021-05-01 for the data seeded into the database will be moved to now
        //     SO THE ABOVE TEST DATES MUST ALSO BE MOVED as per the OffsetDates as set in 010 Repository > ...\Repository\Infrastructure\Extensions.cs
        //     else this test will FAIL.
        public async Task AC05b_Get_WithNewestTime_ShouldRespondWith_200OK_FilteredRecords(string newestTime, int expectedRecordCount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, newestTime: newestTime, expectedRecordCount: expectedRecordCount);
        }

        [Theory]
        [InlineData("10000")]
        public async Task AC06_Get_WithMinAmount_ShouldRespondWith_200OK_FilteredRecords(string minAmount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, minAmount: minAmount);
        }

        [Theory]
        [InlineData("50000")]
        public async Task AC07_Get_WithMinAmount_ShouldRespondWith_200OK_FilteredRecords(string minAmount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, minAmount: minAmount);
        }

        [Theory]
        [InlineData("100")]
        public async Task AC08_Get_WithMaxAmount_ShouldRespondWith_200OK_FilteredRecords(string maxAmount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, maxAmount: maxAmount);
        }

        [Theory]
        [InlineData("5")]
        public async Task AC09_Get_WithMaxAmount_ShouldRespondWith_200OK_FilteredRecords(string maxAmount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, maxAmount: maxAmount);
        }

        [Theory]
        [InlineData("IOU", 2)]
        [InlineData("iou", 2)]
        // NB: If the appsettings.ENV.json > SeedData > OffsetDates = true - then the reference date of 2021-05-01 for the data seeded into the database will be moved to now
        //     THIS TEST WILL FAIL.
        public async Task AC10_Get_WithText_ShouldRespondWith_200OK_FilteredRecords(string text, int expectedRecordCount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, text: text, expectedRecordCount: expectedRecordCount,
                newestTime: DEFAULT_EFFECTIVENEWESTTIME, oldestTime: DEFAULT_EFFECTIVEOLDESTTIME);
        }

        [Theory]
        [InlineData("FOO", 0)]
        public async Task AC11_Get_WithText_ShouldRespondWith_200OK_FilteredRecords(string text, int expectedRecordCount)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, text: text, expectedRecordCount: expectedRecordCount);
        }

        [Theory]
        [InlineData(ACCOUNTID_JANE_WILSON, HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.NotFound)]
        public async Task AC12_Get_WithInvalidAccountId_ShouldRespondWith_404NotFound_ResourceNotFoundErrorResponse(string accountId, HttpStatusCode expectedStatusCode)
        {
            await Test(accountId, TokenType.JANE_WILSON,
                expectedStatusCode: expectedStatusCode,
                expectedErrorResponse: @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Resource/NotFound"",
                            ""title"": ""Resource Not Found"",
                            ""detail"": ""Account ID could not be found for the customer"",
                            ""meta"": {}
                        }]
                    }");
        }

        [Theory]
        [InlineData(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, HttpStatusCode.OK)]
        [InlineData(ACCOUNTID_JOHN_SMITH, TokenType.JANE_WILSON, HttpStatusCode.NotFound)]
        public async Task AC13_Get_WithAccountNotOwnedByCustomer_ShouldRespondWith_404NotFound_ResourceNotFoundErrorResponse(string accountId, TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            await Test(accountId, tokenType,
                expectedStatusCode: expectedStatusCode,
                expectedErrorResponse: @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Resource/NotFound"",
                            ""title"": ""Resource Not Found"",
                            ""detail"": ""Account ID could not be found for the customer"",
                            ""meta"": {}
                        }]
                    }");
        }

        [Theory]
        [InlineData("2020-06-01T00:00:00.000Z", HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC14_Get_WithInvalidField_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse(string newestTime, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                newestTime: newestTime,
                expectedStatusCode: expectedStatusCode,
                expectedErrorResponse: @"{
                    ""errors"": [{
                        ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                        ""title"": ""Invalid Field"",
                        ""detail"": ""The newest-time field is not valid"",
                        ""meta"": {}
                    }]
                }"
            );
        }

        [Theory]
        [InlineData("2021-04-01T00:00:00.000Z", HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC15_Get_WithInvalidField_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse(string oldestTime, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                oldestTime: oldestTime,
                expectedStatusCode: expectedStatusCode,
                expectedErrorResponse: @"{
                    ""errors"": [{
                        ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                        ""title"": ""Invalid Field"",
                        ""detail"": ""The oldest-time field is not valid"",
                        ""meta"": {}
                    }]
                }"
            );
        }

        [Theory]
        [InlineData(SCOPE, HttpStatusCode.OK)]
        [InlineData(SCOPE_WITHOUT_TRANSACTIONSREAD, HttpStatusCode.Forbidden)]
        public async Task AC16_Get_WithoutBankTransactionsReadScope_ShouldRespondWith_403Forbidden(string scope, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
               tokenScope: scope,
               expectedStatusCode: expectedStatusCode,
               expectedErrorResponse: @"{
                    ""errors"": [{
                        ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidConsent"",
                        ""title"": ""Consent Is Invalid"",
                        ""detail"": ""The authorised consumer's consent is insufficient to execute the resource"",
                        ""meta"": {}
                    }]
                }");
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_FOO, HttpStatusCode.Unauthorized, @"Bearer error=""invalid_token""")]
        public async Task AC17_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorised(TokenType tokenType, HttpStatusCode expectedStatusCode, string? expectedWWWAuthenticate = null)
        {
            await Test(ACCOUNTID_JANE_WILSON, tokenType,
                expectedStatusCode: expectedStatusCode,
                expectedWWWAuthenticate: expectedWWWAuthenticate
            );
        }

        [Theory]
        [InlineData(true, HttpStatusCode.Unauthorized, @"Bearer error=""invalid_token"", error_description=""The token expired at ")]
        public async Task AC18_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorised(bool expired, HttpStatusCode expectedStatusCode, string? expectedWWWAuthenticate = null)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                tokenExpired: expired,
                expectedStatusCode: expectedStatusCode,
                expectedWWWAuthenticate: expectedWWWAuthenticate,
                expectedWWWAuthenticateStartsWith: true
            );
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON, HttpStatusCode.OK)]
        [InlineData(TokenType.INVALID_OMIT, HttpStatusCode.Unauthorized, "Bearer")]
        [InlineData(TokenType.INVALID_EMPTY, HttpStatusCode.Unauthorized, "Bearer")]
        public async Task AC20_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorised(TokenType tokenType, HttpStatusCode expectedStatusCode, string? expectedWWWAuthenticate = null)
        {
            await Test(ACCOUNTID_JANE_WILSON, tokenType,
                expectedStatusCode: expectedStatusCode,
                expectedWWWAuthenticate: expectedWWWAuthenticate
            );
        }

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        public async Task AC21_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON); // Ensure token cache is populated before changing status in case InlineData scenarios above are run/debugged out of order

            var saveStatus = GetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID);
            SetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID, status);
            try
            {
                await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                    expectedStatusCode: expectedStatusCode,
                    expectedErrorResponse: $@"{{
                        ""errors"": [{{
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                            ""title"": ""ADR Status Is Not Active"",
                            ""detail"": ""Software product status is { statusDescription }"",
                            ""meta"": {{}}
                        }}]
                    }}");
            }
            finally
            {
                SetStatus(Table.SOFTWAREPRODUCT, SOFTWAREPRODUCT_ID, saveStatus);
            }
        }

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        public async Task AC23_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            var saveStatus = GetStatus(Table.LEGALENTITY, LEGALENTITYID);
            SetStatus(Table.LEGALENTITY, LEGALENTITYID, status);
            try
            {
                await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                    expectedStatusCode: expectedStatusCode,
                    expectedErrorResponse: $@"{{
                        ""errors"": [{{
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive"",
                            ""title"": ""ADR Status Is Not Active"",
                            ""detail"": ""ADR status is { statusDescription }"",
                            ""meta"": {{}}
                        }}]
                    }}");
            }
            finally
            {
                SetStatus(Table.LEGALENTITY, LEGALENTITYID, saveStatus);
            }
        }

        [Theory]
        [InlineData("1", HttpStatusCode.OK)]
        [InlineData("2", HttpStatusCode.NotAcceptable)]
        public async Task AC24_WithXV2_ShouldRespondWith_406NotAcceptable(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                    XV: XV,
                    expectedStatusCode: expectedStatusCode,
                    expectedErrorResponse: @"{
                        ""errors"": [
                            {
                            ""code"": ""urn:au-cds:error:cds-all:Header/UnsupportedVersion"",
                            ""title"": ""Unsupported Version"",
                            ""detail"": ""The minimum supported version is 1. The maximum supported version is 1."",
                            ""meta"": {}
                            }
                        ]}");
        }

        [Theory]
        [InlineData("1", HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData("-1", HttpStatusCode.BadRequest)]
        public async Task AC25_WithInvalidXV_ShouldRespondWith_400BadRequest(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                XV: XV,
                expectedStatusCode: expectedStatusCode,
                expectedErrorResponse: @"{
                    ""errors"": [{
                        ""code"": ""urn:au-cds:error:cds-all:Header/InvalidVersion"",
                        ""title"": ""Invalid Version"",
                        ""detail"": ""Version header must be a positive integer"",
                        ""meta"": {}
                    }]
                }");
        }

        [Theory]
        [InlineData("1", HttpStatusCode.OK)]
        [InlineData("", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC26_WithMissingXV_ShouldRespondWith_400BadRequest(string XV, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                    XV: XV,
                    expectedStatusCode: expectedStatusCode,
                    expectedErrorResponse: @"{
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
        [InlineData(null, HttpStatusCode.BadRequest)]  // omit xfapiauthdate
        public async Task AC27_Get_WithMissingXFAPIAUTHDATE_ShouldRespondWith_400BadRequest(string XFapiAuthDate, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                    XFapiAuthDate: XFapiAuthDate,
                    expectedStatusCode: expectedStatusCode,
                    expectedErrorResponse: @"{
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
        [InlineData("DateTime.UtcNow", HttpStatusCode.BadRequest)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC28_Get_WithInvalidXFAPIAUTHDATE_ShouldRespondWith_400BadRequest(string XFapiAuthDate, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                    XFapiAuthDate: XFapiAuthDate,
                    expectedStatusCode: expectedStatusCode,
                    expectedErrorResponse: @"{
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
        public async Task AC29_Get_WithXFAPIInteractionId123_ShouldRespondWith_200OK_AndXFapiInteractionIDis123(string xFapiInteractionId, HttpStatusCode expectedStatusCode)
        {
            await Test(ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON,
                    XFapiInteractionId: xFapiInteractionId,
                    expectedStatusCode: expectedStatusCode);
        }

        [Theory]
        [InlineData(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, HttpStatusCode.OK)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.Unauthorized)]  // Different holder of key
        public async Task AC30_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorized(string certificateFilename, string certificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = await Fixture.DataHolder_AccessToken_Cache.GetAccessToken(TokenType.JANE_WILSON);

            ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);
            var encryptedAccountId = IdPermanenceEncrypt(ACCOUNTID_JANE_WILSON, customerId, softwareProductId);

            // Act
            var api = new Infrastructure.API
            {
                CertificateFilename = certificateFilename,
                CertificatePassword = certificatePassword,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts/{encryptedAccountId}/transactions",
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
        [InlineData(USERID_JANEWILSON, "98765988", "98765988", HttpStatusCode.OK)] // Retrieving account that has been consented to, should succeed
        [InlineData(USERID_JANEWILSON, "98765988", "98765987", HttpStatusCode.NotFound)] // Retrieving account that has not been consented to, should fail
        public async Task ACX01_Get_WhenConsumerDidNotGrantConsentToAccount_ShouldRespondWith_404NotFound(string userId,
            string consentedAccounts,
            string accountToRetrieve,
            HttpStatusCode expectedStatusCode)
        {
            static async Task<HttpResponseMessage> GetTransactions(string? accessToken, string accountId)
            {
                ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);
                var encryptedAccountId = IdPermanenceEncrypt(accountId, customerId, softwareProductId);

                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts/{encryptedAccountId}/transactions",
                    XV = "1",
                    XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                    AccessToken = accessToken
                };
                var response = await api.SendAsync();

                return response;
            }

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = userId,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = consentedAccounts,
            }.Authorise();

            // Act - Get token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            // Act - Get transactions for account
            var response = await GetTransactions(tokenResponse?.AccessToken, accountToRetrieve);

            // Assert
            response.StatusCode.Should().Be(expectedStatusCode);

            if (expectedStatusCode != HttpStatusCode.OK)
            {
                var expectedResponse = @"{
                    ""errors"": [
                        {
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/UnavailableBankingAccount"",
                            ""title"": ""Unavailable Banking Account"",
                            ""detail"": """",
                            ""meta"": {}
                        }
                    ]
                }";

                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }
    }
}
