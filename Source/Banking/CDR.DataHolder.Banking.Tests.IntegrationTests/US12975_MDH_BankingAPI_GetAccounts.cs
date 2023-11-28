using CDR.DataHolder.Banking.Tests.IntegrationTests.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Dataholders.Banking.Accounts;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Banking.Tests.IntegrationTests
{
    public class US12975_MDH_BankingAPI_GetAccounts : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        protected const string SCOPE_WITHOUT_ACCOUNTSBASICREAD = "openid common:customer.basic:read bank:transactions:read";
        private readonly TestAutomationOptions _options;
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US12975_MDH_BankingAPI_GetAccounts(IOptions<TestAutomationOptions> options,
            IDataHolderAccessTokenCache dataHolderAccessTokenCache,
            ISqlQueryService sqlQueryService,
            IDataHolderTokenService dataHolderTokenService,
            IDataHolderParService dataHolderParService,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config,
            RegisterSoftwareProductFixture registerSoftwareProductFixture) : base(testOutputHelperAccessor, config)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _dataHolderAccessTokenCache = dataHolderAccessTokenCache ?? throw new ArgumentNullException(nameof(dataHolderAccessTokenCache));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
            if (registerSoftwareProductFixture == null)
            {
                throw new ArgumentNullException(nameof(registerSoftwareProductFixture));
            }
        }

        [Theory]
        [InlineData(TokenType.JaneWilson)]
        [InlineData(TokenType.KamillaSmith)]
        [InlineData(TokenType.Beverage, Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51320")]
        public async Task AC01_Get_ShouldRespondWith_200OK_Accounts(TokenType tokenType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(tokenType), tokenType);

            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType);
        }

        [Fact]
        public async Task AC02_Get_WithPageSize5_ShouldRespondWith_200OK_Page1Of5Records()
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(TokenType.KamillaSmith, queryPage: 1, queryPageSize: 5);
        }

        [Fact]
        public async Task AC03_Get_WithPageSize5_AndPage3_ShouldRespondWith_200OK_Page3Of5Records()
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(TokenType.KamillaSmith, queryPage: 3, queryPageSize: 5);
        }

        [Fact]
        public async Task AC04_Get_WithPageSize5_AndPage5_ShouldRespondWith_200OK_Page5Of5Records()
        {
            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(TokenType.KamillaSmith, queryPage: 5, queryPageSize: 5);
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, 0)]
        public async Task AC05_Get_WithIsOwnedFalse_ShouldRespondWith_200OK_NonOwnedAccounts(TokenType tokenType, int expectedRecordCount)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedRecordCount), expectedRecordCount);

            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType, isOwned: false, expectedRecordCount: expectedRecordCount, queryPage: 1, queryPageSize: 5);
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, 9)]
        public async Task AC06_Get_WithOpenStatusCLOSED_ShouldRespondWith_200OK_ClosedAccounts(TokenType tokenType, int expectedRecordCount)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedRecordCount), expectedRecordCount);

            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType, openStatus: "CLOSED", expectedRecordCount: expectedRecordCount, queryPage: 1, queryPageSize: 5);
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, "OPEN", "PERS_LOANS", 7)]
        [InlineData(TokenType.KamillaSmith, "OPEN", "BUSINESS_LOANS", 0)]
        [InlineData(TokenType.Business1, "OPEN", "BUSINESS_LOANS", 1, Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51320")]
        [InlineData(TokenType.Beverage, "CLOSED", "BUSINESS_LOANS", 1, Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51320")]
        public async Task AC07_Get_WithOpenStatusOPEN_AndProductCategoryBUSINESSLOANS_ShouldRespondWith_200OK_OpenedBusinessLoanAccounts(TokenType tokenType, string openStatus, string productCategory, int expectedRecordCount)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}, {P4}={V4}.", nameof(tokenType), tokenType, nameof(openStatus), openStatus, nameof(productCategory), productCategory, nameof(expectedRecordCount), expectedRecordCount);

            await Test_AC01_AC02_AC04_AC04_AC05_AC06_AC07(tokenType, openStatus: openStatus, productCategory: productCategory, expectedRecordCount: expectedRecordCount, queryPage: 1, queryPageSize: 5);
        }

        [Fact]
        public async Task AC08_Get_WithBankAccountsReadScope_Success()
        {
            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith, _options.SCOPE);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(SCOPE_WITHOUT_ACCOUNTSBASICREAD)]
        public async Task AC08_Get_WithoutBankAccountsReadScope_ShouldRespondWith_403Forbidden(string scope)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(scope), scope);

            if (String.IsNullOrEmpty(scope))
            {
                scope = _options.SCOPE;
            }

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith, scope);

            CdrException expectedError = new InvalidConsentException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);
                {
                    // Assert - Check application/json
                    Assertions.AssertHasContentTypeApplicationJson(response.Content);

                    // Assert - Check error response
                    await Assertions.AssertHasContentJson(expectedContent, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC09_Get_WithValidAccessToken_Success()
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(TokenType.InvalidFoo)]
        public async Task AC09_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorized(TokenType tokenType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(tokenType), tokenType);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            CdrException expectedError = new InvalidTokenException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, HttpStatusCode.OK)]
        [InlineData(TokenType.InvalidEmpty, HttpStatusCode.Unauthorized)]
        [InlineData(TokenType.InvalidOmit, HttpStatusCode.Unauthorized)]
        public async Task AC11_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorized(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedStatusCode), expectedStatusCode);

            await Test_AC09_AC11(tokenType, expectedStatusCode, "Bearer");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task AC10_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized(HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            var accessToken = Constants.AccessTokens.ConsumerAccessTokenEnergyExpired;

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check error response
                    Assertions.AssertHasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        response.Headers,
                        "WWW-Authenticate",
                        true); // starts with
                }
            }
        }

#pragma warning disable xUnit1004
        [Theory(Skip = "Skipping this as the test (before refactoring) was giving false positives and we should reassess what this test should do")]
        [InlineData(SoftwareProductStatus.INACTIVE)]
        [InlineData(SoftwareProductStatus.REMOVED)]
        public async Task AC12_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(SoftwareProductStatus status)
        {
            //TODO: Reassess if this test is correct (also, why are we prefixing ERR-GEN-002?) - Bug 63710
            await Test_AC12_AC13_AC14(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, status.ToEnumMemberAttrValue(), new AdrStatusNotActiveException(status));
        }

        [Theory(Skip = "Skipping this as the test (before refactoring) was giving false positives and we should reassess what this test should do")]
        [InlineData(LegalEntityStatus.INACTIVE)]
        [InlineData(LegalEntityStatus.REMOVED)]
        public async Task AC14_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(LegalEntityStatus status)
        {
            //TODO: Reassess if this test is correct (also, why are we prefixing ERR-GEN-002?..and does the message make sense when the Legal Entity is the non-active one?) - Bug 63710
            await Test_AC12_AC13_AC14(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, status.ToEnumMemberAttrValue(), new AdrStatusNotActiveException(status));
        }
#pragma warning restore xUnit1004

        [Fact]
        public async Task AC15_Get_WithXV2_ShouldRespondWith_406NotAcceptable()
        {
            await Test_AC15_AC16_AC17("2", new UnsupportedVersionException());
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("99999999999999999999999999999999999999999999999999")]
        [InlineData("-1")]
        public async Task AC16_Get_WithInvalidXV_ShouldRespondWith_400BadRequest(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            await Test_AC15_AC16_AC17(xv, new InvalidVersionException());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task AC17_Get_WithMissingXV_ShouldRespondWith_400BadRequest(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            await Test_AC15_AC16_AC17(xv, new MissingRequiredHeaderException("x-v"));
        }

        [Theory]
        //[InlineData("DateTime.Now.RFC1123", HttpStatusCode.OK)]
        [InlineData(null)]  // omit xfapiauthdate
        public async Task AC18_Get_WithMissingXFAPIAUTHDATE_ShouldRespondWith_400BadRequest(string xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            await Test_AC18_AC19(DateTimeExtensions.GetDateFromFapiDate(xFapiAuthDate), new MissingRequiredHeaderException("x-fapi-auth-date"));
        }

        [Theory]
        //[InlineData("DateTime.Now.RFC1123", HttpStatusCode.OK)]
        [InlineData("DateTime.UtcNow")]
        [InlineData("foo")]
        public async void AC19_Get_WithInvalidXFAPIAUTHDATE_ShouldRespondWith_400BadRequest(string xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            await Test_AC18_AC19(
                ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions.DateTimeExtensions.GetDateFromFapiDate(xFapiAuthDate),
                new InvalidHeaderException("x-fapi-auth-date"));
        }

        [Theory]
        [InlineData("123", HttpStatusCode.OK)]
        public async Task AC20_Get_WithXFAPIInteractionId123_ShouldRespondWith_200OK_Accounts_AndXFapiInteractionIDis123(string xFapiInteractionId, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(xFapiInteractionId), xFapiInteractionId, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"), "1", xFapiInteractionId: xFapiInteractionId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check x-fapi-interaction-id header
                Assertions.AssertHasHeader(xFapiInteractionId, response.Headers, "x-fapi-interaction-id");
            }
        }

        [Theory]
        [InlineData(Constants.Certificates.CertificateFilename, Constants.Certificates.CertificatePassword)]
        public async Task AC21_Get_WithCorrectHolderOfKey_Success(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"), xv: "1", certFileName: certificateFilename, certPassword: certificatePassword);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(Constants.Certificates.AdditionalCertificateFilename, Constants.Certificates.AdditionalCertificatePassword)]  // Different holder of key
        public async Task AC21_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorized(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            var expectedError = new InvalidTokenException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"), xv: "1", certFileName: certificateFilename, certPassword: certificatePassword);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData(Constants.Users.Banking.UserIdJaneWilson, "98765988,98765987")] // All accounts
        [InlineData(Constants.Users.Banking.UserIdJaneWilson, "98765988")] // Subset of accounts
        public async Task ACX01_Get_WhenConsumerDidNotGrantConsentToAllAccounts_ShouldRespondWith_200OK_ConsentedAccounts(string userId, string consentedAccounts)
        {
            async Task<ResponseBankingAccountListV2?> GetAccounts(string? accessToken)
            {
                var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
                var response = await api.SendAsync();

                if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Error getting accounts");

                var json = await response.Content.ReadAsStringAsync();

                var accountsResponse = JsonConvert.DeserializeObject<ResponseBankingAccountListV2>(json);

                return accountsResponse;
            }

            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(userId), userId, nameof(consentedAccounts), consentedAccounts);

            var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector)
              .WithUserId(userId)
              .WithSelectedAccountIds(consentedAccounts)
              .WithResponseMode(ResponseMode.FormPost)
              .BuildAsync();


            (var authCode, _) = await authService.Authorise();

            // Act
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);
            var accountsResponse = await GetAccounts(tokenResponse?.AccessToken);

            Helpers.ExtractClaimsFromToken(tokenResponse?.AccessToken, out var loginId, out var softwareProductId);
            var encryptedAccountIds = consentedAccounts.Split(',').Select(consentedAccountId => Helpers.IdPermanenceEncrypt(consentedAccountId, loginId, softwareProductId));

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check each account in response is one of the consented accounts
                foreach (var account in accountsResponse?.Data?.Accounts ?? throw new NullReferenceException())
                {
                    encryptedAccountIds.Should().Contain(account.AccountId);
                }
            }
        }

        [Theory]
        [InlineData(Constants.Users.Banking.UserIdJaneWilson, "98765988,98765987")] // All accounts
        public async Task ACX02_GetAccountsMultipleTimes_ShouldRespondWith_SameEncryptedAccountIds(string userId, string consentedAccounts)
        {
            async Task<string?[]?> GetAccountIds(string userId, string consentedAccounts)
            {
                async Task<ResponseBankingAccountListV2?> GetAccounts(string? accessToken)
                {
                    var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
                    var response = await api.SendAsync();

                    if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Error getting accounts");

                    var json = await response.Content.ReadAsStringAsync();

                    var accountsResponse = JsonConvert.DeserializeObject<ResponseBankingAccountListV2>(json);

                    return accountsResponse;
                }

                // Get authcode
                var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector)
            .WithUserId(userId)
            .WithSelectedAccountIds(consentedAccounts)
            .WithResponseMode(ResponseMode.FormPost)
            .BuildAsync();

                (var authCode, _) = await authService.Authorise();

                // Get token
                var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);

                // Get accounts
                var accountsResponse = await GetAccounts(tokenResponse?.AccessToken);

                // Return list of account ids
                return accountsResponse?.Data?.Accounts?.Select(x => x.AccountId).ToArray();
            }

            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(userId), userId, nameof(consentedAccounts), consentedAccounts);

            // Act - Get accounts
            var encryptedAccountIDs1 = await GetAccountIds(userId, consentedAccounts);

            // Act - Get accounts again
            var encryptedAccountIDs2 = await GetAccountIds(userId, consentedAccounts);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                encryptedAccountIDs1.Should().NotBeNullOrEmpty();
                encryptedAccountIDs2.Should().NotBeNullOrEmpty();

                // Assert - Encrypted account ids should be same
                encryptedAccountIDs1.Should().BeEquivalentTo(encryptedAccountIDs2);
            }
        }

        private static (string, int) GetExpectedResponse(string? accessToken,
            string baseUrl, string selfUrl,
            bool? isOwned = null, string? openStatus = null, string? productCategory = null,
            int? page = null, int? pageSize = null)
        {
            Helpers.ExtractClaimsFromToken(accessToken, out var loginId, out var softwareProductId);

            var effectivePage = page ?? 1;
            var effectivePageSize = pageSize ?? 25;

            string seedDataJson = File.ReadAllText("TestData/seed-data.json");
            var seedData = JsonConvert.DeserializeObject<BankingSeedData>(seedDataJson);

            // NB: This has to compare decrypted Id's as AES Encryption now uses a Random IV,
            //     using encrypted ID's in the response and expected content WILL NEVER MATCH
            var currentCustomer = seedData?.Customers.Where(c => c.LoginId == loginId).FirstOrDefault();

            var accounts = currentCustomer?.Accounts?
                .Select(account => new
                {
                    accountId = Helpers.IdPermanenceEncrypt(account.AccountId, loginId, softwareProductId),
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
            accounts = accounts?
                .Where(account => isOwned == null || account.isOwned == isOwned)
                .Where(account => openStatus == null || account.openStatus == openStatus)
                .Where(account => productCategory == null || account.productCategory == productCategory)
                .ToList();

            var totalRecords = accounts == null ? 0 : accounts.Count;

            // Paging
            accounts = accounts?
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
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            var baseUrl = $"{_options.DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts";
            var url = GetUrl(baseUrl, isOwned, openStatus, productCategory, queryPage, queryPageSize);

            (var expectedResponse, var totalRecords) = GetExpectedResponse(accessToken, baseUrl, url,
                isOwned, openStatus, productCategory,
                queryPage, queryPageSize);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"), url: url);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check XV
                Assertions.AssertHasHeader(api.XV, response.Headers, "x-v");

                // Assert - Check x-fapi-interaction-id
                Assertions.AssertHasHeader(null, response.Headers, "x-fapi-interaction-id");

                // Assert - Check json
                await Assertions.AssertHasContentJson(expectedResponse, response.Content);

                // Assert - Record count
                if (expectedRecordCount != null)
                {
                    totalRecords.Should().Be(expectedRecordCount);
                }
            }
        }

        private async Task Test_AC09_AC11(TokenType tokenType, HttpStatusCode expectedStatusCode, string expectedWWWAuthenticateResponse)
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check error response 
                    Assertions.AssertHasHeader(expectedWWWAuthenticateResponse, response.Headers, "WWW-Authenticate");
                }
            }
        }

        private async Task Test_AC12_AC13_AC14(EntityType entityType, string id, string status, CdrException expectedError)
        {
            var saveStatus = _sqlQueryService.GetStatus(entityType, id);
            _sqlQueryService.SetStatus(entityType, id, status);

            try
            {
                var accessToken = string.Empty;

                // Arrange
                try
                {
                    accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith); // Ensure token cache is populated before changing status in case InlineData scenarios above are run/debugged out of order
                }
                catch (AuthoriseException ex)
                {
                    // Assert
                    using (new AssertionScope(BaseTestAssertionStrategy))
                    {
                        // Assert - Check error response
                        ex.StatusCode.Should().Be(expectedError.StatusCode);
                        ex.Error.Should().Be(expectedError.Code);
                        ex.ErrorDescription.Should().Be(expectedError.Detail);

                        return;
                    }
                }

                // Act
                var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"));
                var response = await api.SendAsync();

                // Assert
                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedError.StatusCode);
                }
            }
            finally
            {
                _sqlQueryService.SetStatus(entityType, id, saveStatus);
            }
        }

        private async Task Test_AC15_AC16_AC17(string xv, CdrException expectedError)
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: DateTime.Now.ToUniversalTime().ToString("r"), xv: xv);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check content type 
                    Assertions.AssertHasContentTypeApplicationJson(response.Content);

                    // Assert - Check error response
                    await Assertions.AssertHasContentJson(expectedContent, response.Content);
                }
            }
        }

        private async Task Test_AC18_AC19(string? xFapiAuthDate, CdrException expectedError)
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetAccountsAPI(accessToken, xFapiAuthDate: xFapiAuthDate);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check content type 
                    Assertions.AssertHasContentTypeApplicationJson(response.Content);

                    // Assert - Check error response
                    await Assertions.AssertHasContentJson(expectedContent, response.Content);
                }
            }
        }
    }
}
