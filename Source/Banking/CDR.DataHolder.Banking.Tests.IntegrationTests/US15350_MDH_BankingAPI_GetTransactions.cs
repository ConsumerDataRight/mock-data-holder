#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using CDR.DataHolder.Banking.Tests.IntegrationTests.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
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
using TokenType = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums.TokenType;

namespace CDR.DataHolder.Banking.Tests.IntegrationTests
{
    public class US15350_MDH_BankingAPI_GetTransactions : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private const string SCOPE_WITHOUT_TRANSACTIONSREAD = "openid common:customer.basic:read bank:accounts.basic:read";
        private const string FOO_GUID = "F0000000-F000-F000-F000-F00000000000";

        // Note: These default dates are based on the current seed-data.json file to select a valid data set.
        private static string DEFAULT_EFFECTIVENEWESTTIME => "2022-06-01T00:00:00Z";
        private static string DEFAULT_EFFECTIVEOLDESTTIME => "2022-03-01T00:00:00Z";

        private readonly TestAutomationOptions _options;
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US15350_MDH_BankingAPI_GetTransactions(IOptions<TestAutomationOptions> options,
            IDataHolderAccessTokenCache dataHolderAccessTokenCache,
            ISqlQueryService sqlQueryService,
            IDataHolderTokenService dataHolderTokenService,
            IDataHolderParService dataHolderParService,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config,
            RegisterSoftwareProductFixture registerSoftwareProductFixture)
            : base(testOutputHelperAccessor, config)
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

        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_Transactions()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters());
        }

        [Fact]
        public async Task AC02_Get_WithPageSize5_ShouldRespondWith_200OK_Page1Of5Records()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { PageSize = 5 });
        }

        [Fact]
        public async Task AC03_Get_WithPageSize5_AndPage3_ShouldRespondWith_200OK_Page3Of5Records()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { Page = 3, PageSize = 5 });
        }

        [Fact]
        public async Task AC04_Get_WithPageSize5_AndPage5_ShouldRespondWith_200OK_Page5Of5Records()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { Page = 5, PageSize = 5 });
        }

        [Theory]
        [InlineData("2022-05-25T00:00:00.000Z", 2)]
        [InlineData("2022-05-26T00:00:00.000Z", 0)]
        // NB: If the appsettings.ENV.json > SeedData > OffsetDates = true - then the reference date of 2022-05-01 for the data seeded into the database will be moved to now
        //     SO THE ABOVE TEST DATES MUST ALSO BE MOVED as per the OffsetDates as set in 010 Repository > ...\Repository\Infrastructure\Extensions.cs
        //     else this test will FAIL.
        public async Task AC05_Get_WithOldestTime_ShouldRespondWith_200OK_FilteredRecords(string oldestTime, int expectedRecordCount)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(oldestTime), oldestTime, nameof(expectedRecordCount), expectedRecordCount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { OldestTime = oldestTime }, expectedRecordCount);
        }

        [Theory]
        [InlineData("2022-03-01T00:00:00.000Z", 0)]
        [InlineData("2022-03-02T00:00:00.000Z", 2)]
        // NB: If the appsettings.ENV.json > SeedData > OffsetDates = true - then the reference date of 2022-05-01 for the data seeded into the database will be moved to now
        //     SO THE ABOVE TEST DATES MUST ALSO BE MOVED as per the OffsetDates as set in 010 Repository > ...\Repository\Infrastructure\Extensions.cs
        //     else this test will FAIL.
        public async Task AC05b_Get_WithNewestTime_ShouldRespondWith_200OK_FilteredRecords(string newestTime, int expectedRecordCount)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(newestTime), newestTime, nameof(expectedRecordCount), expectedRecordCount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { NewestTime = newestTime }, expectedRecordCount);
        }

        [Theory]
        [InlineData("10000")]
        public async Task AC06_Get_WithMinAmount_ShouldRespondWith_200OK_FilteredRecords(string minAmount)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(minAmount), minAmount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { MinAmount = minAmount });
        }

        [Theory]
        [InlineData("50000")]
        public async Task AC07_Get_WithMinAmount_ShouldRespondWith_200OK_FilteredRecords(string minAmount)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(minAmount), minAmount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { MinAmount = minAmount });
        }

        [Theory]
        [InlineData("100")]
        public async Task AC08_Get_WithMaxAmount_ShouldRespondWith_200OK_FilteredRecords(string maxAmount)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(maxAmount), maxAmount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { MaxAmount = maxAmount });
        }

        [Theory]
        [InlineData("5")]
        public async Task AC09_Get_WithMaxAmount_ShouldRespondWith_200OK_FilteredRecords(string maxAmount)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(maxAmount), maxAmount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { MaxAmount = maxAmount });
        }

        [Theory]
        [InlineData("IOU", 2)]
        [InlineData("iou", 2)]
        // NB: If the appsettings.ENV.json > SeedData > OffsetDates = true - then the reference date of 2022-05-01 for the data seeded into the database will be moved to now
        //     THIS TEST WILL FAIL.
        public async Task AC10_Get_WithText_ShouldRespondWith_200OK_FilteredRecords(string text, int expectedRecordCount)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(text), text, nameof(expectedRecordCount), expectedRecordCount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { Text = text }, expectedRecordCount);
        }

        [Theory]
        [InlineData("FOO", 0)]
        public async Task AC11_Get_WithText_ShouldRespondWith_200OK_FilteredRecords(string text, int expectedRecordCount)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(text), text, nameof(expectedRecordCount), expectedRecordCount);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { Text = text }, expectedRecordCount);
        }

        [Fact]
        public async Task AC12_Get_WithValidAccountId_Success()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters());
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC12_Get_WithInvalidAccountId_ShouldRespondWith_404NotFound_ResourceNotFoundErrorResponse(string accountId)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(accountId), accountId);

            await TestForError(TokenType.JaneWilson, accountId, new ResourceNotFoundException("Account ID could not be found for the customer"), new TransactionFilterParameters());
        }

        [Theory]
        [InlineData("2020-06-01T00:00:00.000Z")]
        public async Task AC14_Get_WithValidNewestTime_Success(string newestTime)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(newestTime), newestTime);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { NewestTime = newestTime });
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC14_Get_WithInvalidField_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse_(string newestTime)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(newestTime), newestTime);

            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidFieldException("The newest-time field is not valid"), new TransactionFilterParameters { NewestTime = newestTime });
        }

        [Theory]
        [InlineData("2022-04-01T00:00:00.000Z")]
        public async Task AC15_Get_WithValidField_Success(string oldestTime)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(oldestTime), oldestTime);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters { OldestTime = oldestTime });
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC15_Get_WithInvalidField_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse(string oldestTime)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(oldestTime), oldestTime);

            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidFieldException("The oldest-time field is not valid"), new TransactionFilterParameters { OldestTime = oldestTime });
        }

        [Fact]
        public async Task AC16_Get_DefaultScope_Success()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters(), scope: _options.SCOPE);
        }

        [Theory]
        [InlineData(SCOPE_WITHOUT_TRANSACTIONSREAD)]
        public async Task AC16_Get_WithoutBankTransactionsReadScope_ShouldRespondWith_403Forbidden(string scope)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(scope), scope);

            if (string.IsNullOrEmpty(scope))
            {
                scope = _options.SCOPE;
            }

            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidConsentException(), new TransactionFilterParameters(), scope: scope);
        }

        [Fact]
        public async Task AC17_Get_WithValidAccessToken_Success()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters());
        }

        [Theory]
        [InlineData(TokenType.InvalidFoo, @"Bearer error=""invalid_token""")]
        public async Task AC17_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorised(TokenType tokenType, string? expectedWWWAuthenticate = null)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedWWWAuthenticate), expectedWWWAuthenticate);

            await TestForError(tokenType, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidTokenException(), new TransactionFilterParameters(), expectedWWWAuthenticate: expectedWWWAuthenticate);
        }

        [Theory]
        [InlineData(@"Bearer error=""invalid_token"", error_description=""The token expired at ")]
        public async Task AC18_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorised(string? expectedWWWAuthenticate = null)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(expectedWWWAuthenticate), expectedWWWAuthenticate);

            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidTokenException(), new TransactionFilterParameters(), expectedWWWAuthenticate: expectedWWWAuthenticate, expectedWWWAuthenticateStartsWith: true, tokenExpired: true);
        }

        [Fact]
        public async Task AC20_Get_WithValidAccessToken_Success()
        {
            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters());
        }

        [Theory]
        [InlineData(TokenType.InvalidOmit, "Bearer")]
        [InlineData(TokenType.InvalidEmpty, "Bearer")]
        public async Task AC20_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorised(TokenType tokenType, string? expectedWWWAuthenticate = null)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedWWWAuthenticate), expectedWWWAuthenticate);

            await TestForError(tokenType, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidTokenException(), new TransactionFilterParameters(), expectedWWWAuthenticate: expectedWWWAuthenticate);
        }

#pragma warning disable xUnit1004
        [Theory(Skip = "This test is accurate but is failing due to a bug. Prefer to skip it for now rather than test for incorrect behaviour")]
        [InlineData(SoftwareProductStatus.INACTIVE)]
        [InlineData(SoftwareProductStatus.REMOVED)]
        public async Task AC21_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(SoftwareProductStatus status)
        {
            //TODO: This is failing, but the test is correct. The old test checked for a 200Ok and we may need to do that in the short term to get the test passing (or skip it). Bug 63702
            var saveStatus = _sqlQueryService.GetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId);
            _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, status.ToEnumMemberAttrValue());
            try
            {
                await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new AdrStatusNotActiveException(status), new TransactionFilterParameters(), useCache: false);
                //await Test(Constants.Accounts.Banking.ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, expectedStatusCode: HttpStatusCode.OK, expectedErrorResponse: $"ERR-GEN-002: Software product status is {status}", useCache: false);
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, saveStatus);
            }
        }
#pragma warning restore xUnit1004

        //TODO: AC22 is missing tests

        [Theory]
        [InlineData(LegalEntityStatus.ACTIVE)]
        public async Task AC23_Get_WithADRParticipationActive_Success(LegalEntityStatus status)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(status), status);

            var saveStatus = _sqlQueryService.GetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId);
            _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, status.ToEnumMemberAttrValue());
            try
            {
                await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters());
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, saveStatus);
            }
        }

#pragma warning disable xUnit1004
        [Theory(Skip = "This test is accurate but is failing due to a bug. Prefer to skip it for now rather than test for incorrect behaviour")]
        [InlineData(LegalEntityStatus.INACTIVE)]
        [InlineData(LegalEntityStatus.REMOVED)]
        public async Task AC23_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(LegalEntityStatus status)
        {
            //TODO: This is failing, but the test is correct. Raise a bug
            var saveStatus = _sqlQueryService.GetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId);
            _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, status.ToEnumMemberAttrValue());
            try
            {
                await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new AdrStatusNotActiveException(status), new TransactionFilterParameters(), useCache: false);
                //await Test(Constants.Accounts.Banking.ACCOUNTID_JANE_WILSON, TokenType.JANE_WILSON, expectedErrorResponse: $"ERR-GEN-002: Software product status is {status}", useCache: false);
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, saveStatus);
            }
        }
#pragma warning restore xUnit1004

        [Fact]
        public async Task AC24_WithXV2_ShouldRespondWith_406NotAcceptable()
        {
            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new UnsupportedVersionException(), new TransactionFilterParameters(), xv: "2");
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("-1")]
        public async Task AC25_WithInvalidXV_ShouldRespondWith_400BadRequest(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidVersionException(), new TransactionFilterParameters(), xv: xv);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task AC26_WithMissingXV_ShouldRespondWith_400BadRequest(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new MissingRequiredHeaderException("x-v"), new TransactionFilterParameters(), xv: xv);
        }

        [Fact]
        public async Task AC27_Get_WithMissingXFAPIAUTHDATE_ShouldRespondWith_400BadRequest()
        {
            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new MissingRequiredHeaderException("x-fapi-auth-date"), new TransactionFilterParameters(), xFapiAuthDate: null);
        }

        [Theory]
        [InlineData("DateTime.UtcNow")]
        [InlineData("foo")]
        public async Task AC28_Get_WithInvalidXFAPIAUTHDATE_ShouldRespondWith_400BadRequest(string xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            await TestForError(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new InvalidHeaderException("x-fapi-auth-date"), new TransactionFilterParameters(), xFapiAuthDate: xFapiAuthDate);
        }

        [Theory]
        [InlineData("123")]
        public async Task AC29_Get_WithXFAPIInteractionId123_ShouldRespondWith_200OK_AndXFapiInteractionIDis123(string xFapiInteractionId)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiInteractionId), xFapiInteractionId);

            await TestForSuccess(TokenType.JaneWilson, Constants.Accounts.Banking.AccountIdJaneWilson, new TransactionFilterParameters(), xFapiInteractionId: xFapiInteractionId);
        }

        [Fact]
        public async Task AC30_Get_WithCorrectHolderOfKey_Success()
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            Helpers.ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);
            var encryptedAccountId = Helpers.IdPermanenceEncrypt(Constants.Accounts.Banking.AccountIdJaneWilson, customerId, softwareProductId);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetTransactionsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), encryptedAccountId: encryptedAccountId, certFileName: Constants.Certificates.CertificateFilename, certPassword: Constants.Certificates.CertificatePassword);
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
        public async Task AC30_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorized(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            Helpers.ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);
            var encryptedAccountId = Helpers.IdPermanenceEncrypt(Constants.Accounts.Banking.AccountIdJaneWilson, customerId, softwareProductId);

            var expectedError = new InvalidTokenException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError, string.Empty));

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetTransactionsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), encryptedAccountId: encryptedAccountId, certFileName: certificateFilename, certPassword: certificatePassword);
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
        [InlineData(Constants.Users.Banking.UserIdJaneWilson, "98765988", "98765988")] // Retrieving account that has been consented to, should succeed
        public async Task ACX01_Get_WhenConsumerDidGrantConsentToAccount_Success(string userId,
           string consentedAccounts,
           string accountToRetrieve)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(userId), userId, nameof(consentedAccounts), consentedAccounts, nameof(accountToRetrieve), accountToRetrieve);

            // Arrange - Get authcode
            var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector)
            .WithUserId(userId)
            .WithSelectedAccountIds(consentedAccounts)
            .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Act - Get token
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);

            // Act - Get transactions for account
            var response = await GetTransactionsForAccountId(tokenResponse?.AccessToken, accountToRetrieve);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

#pragma warning disable xUnit1004
        [Theory(Skip = "This test case matches current requirements but has identified a bug, so we are skipping it until the bug is resolved")]
        [InlineData(Constants.Users.Banking.UserIdJaneWilson, "98765988", "98765987")] // Retrieving account that has not been consented to, should fail
        public async Task ACX01_Get_WhenConsumerDidNotGrantConsentToAccount_ShouldRespondWith_404NotFound(string userId,
            string consentedAccounts,
            string accountToRetrieve)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(userId), userId, nameof(consentedAccounts), consentedAccounts, nameof(accountToRetrieve), accountToRetrieve);

            //TODO: This is failing but the test is correct. Fails as it gets a 404 Notfound when it shold be a 422 UnprocessableEntity. Bug 63707

            // Arrange - Get authcode
            var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector)
            .WithUserId(userId)
            .WithSelectedAccountIds(consentedAccounts)
            .WithResponseMode(ResponseMode.FormPost)
            .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            var expectedError = new UnavailableBankingAccountException(accountToRetrieve);
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError, string.Empty));

            // Act - Get token
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);

            // Act - Get transactions for account
            var response = await GetTransactionsForAccountId(tokenResponse?.AccessToken, accountToRetrieve);

            // Assert
            response.StatusCode.Should().Be(expectedError.StatusCode);

            await Assertions.AssertHasContentJson(expectedContent, response.Content);
        }
#pragma warning disable xUnit1004

        private async Task<HttpResponseMessage> GetTransactionsForAccountId(string? accessToken, string accountId)
        {
            Helpers.ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);
            var encryptedAccountId = Helpers.IdPermanenceEncrypt(accountId, customerId, softwareProductId);

            var api = _apiServiceDirector.BuildDataHolderBankingGetTransactionsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), encryptedAccountId: encryptedAccountId);
            var response = await api.SendAsync();

            return response;
        }

        private class TransactionFilterParameters
        {
            public string? OldestTime { get; init; }
            public string? NewestTime { get; init; }
            public string? MinAmount { get; init; }
            public string? MaxAmount { get; init; }
            public int? Page { get; init; } = 1;
            public int? PageSize { get; init; }
            public string? Text { get; init; }
        }

        private static (string, int) GetExpectedResponse(
           string accountId, string? accessToken,
           string baseUrl, string selfUrl,
           TransactionFilterParameters filterParams)
        {
            //calculate defaults to use for filtering if some required params are missing (don't make the properties required, as we want to know they were defaulted so we can exclude them from response url)
            var effectivePage = filterParams.Page ?? 1;
            var effectivePageSize = filterParams.PageSize ?? 25;
            var effectiveNewestTime = filterParams.NewestTime ?? DEFAULT_EFFECTIVENEWESTTIME;
            var effectiveOldestTime = filterParams.OldestTime ?? DEFAULT_EFFECTIVEOLDESTTIME;

            Helpers.ExtractClaimsFromToken(accessToken, out var loginId, out var softwareProductId);

            //using var dbContext = new DataHolderDatabaseContext(new DbContextOptionsBuilder<DataHolderDatabaseContext>().UseSqlServer(_options.DATAHOLDER_CONNECTIONSTRING).Options);

            string seedDataJson = File.ReadAllText("TestData/seed-data.json");
            var seedData = JsonConvert.DeserializeObject<BankingSeedData>(seedDataJson);

            // NB: This has to compare decrypted Id's as AES Encryption now uses a Random IV,
            //     using encrypted ID's in the response and expected content WILL NEVER MATCH
            var currentCustomer = seedData?.Customers.Where(c => c.LoginId == loginId).FirstOrDefault();
            var accounts = currentCustomer?.Accounts?.Where(account => account.AccountId == accountId);

            var transactions = accounts?.FirstOrDefault()?.Transactions?
                .Select(transaction => new
                {
                    accountId = Helpers.IdPermanenceEncrypt(accountId, loginId, softwareProductId),
                    transactionId = Helpers.IdPermanenceEncrypt(transaction.TransactionId, loginId, softwareProductId),
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
                    crn = transaction.CRN,
                    apcaNumber = transaction.ApcaNumber,
                })
                .ToList();

            // Filter
            transactions = transactions?
                .Where(transaction => (transaction.postingDateTime ?? transaction.executionDateTime) >= DateTime.Parse(effectiveOldestTime).ToUniversalTime())
                .Where(transaction => (transaction.postingDateTime ?? transaction.executionDateTime) <= DateTime.Parse(effectiveNewestTime).ToUniversalTime())
                .Where(transaction => filterParams.MinAmount == null || Decimal.Parse(transaction.amount) >= Decimal.Parse(filterParams.MinAmount))
                .Where(transaction => filterParams.MaxAmount == null || Decimal.Parse(transaction.amount) <= Decimal.Parse(filterParams.MaxAmount))
                .Where(transaction => filterParams.Text == null ||
                    transaction.description.Contains(filterParams.Text, StringComparison.InvariantCultureIgnoreCase) ||
                    transaction.reference.Contains(filterParams.Text, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            var totalRecords = transactions == null ? 0 : transactions.Count;

            // Paging
            transactions = transactions?
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
                    first = totalPages == 0 ? null : GetUrl(baseUrl, filterParams.OldestTime, filterParams.NewestTime, filterParams.MinAmount, filterParams.MaxAmount, filterParams.Text, 1, effectivePageSize, true),
                    last = totalPages == 0 ? null : GetUrl(baseUrl, filterParams.OldestTime, filterParams.NewestTime, filterParams.MinAmount, filterParams.MaxAmount, filterParams.Text, totalPages, effectivePageSize, true),
                    next = totalPages == 0 || effectivePage == totalPages ? null : GetUrl(baseUrl, filterParams.OldestTime, filterParams.NewestTime, filterParams.MinAmount, filterParams.MaxAmount, filterParams.Text, effectivePage + 1, effectivePageSize, true),
                    prev = totalPages == 0 || effectivePage == 1 ? null : GetUrl(baseUrl, filterParams.OldestTime, filterParams.NewestTime, filterParams.MinAmount, filterParams.MaxAmount, filterParams.Text, effectivePage - 1, effectivePageSize, true),
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

        static string GetUrl(string baseUrl,
            TransactionFilterParameters filterParams,
            bool isLink = false)
        {
            return GetUrl(baseUrl, filterParams.OldestTime, filterParams.NewestTime, filterParams.MinAmount, filterParams.MaxAmount, filterParams.Text, filterParams.Page, filterParams.PageSize, isLink);
        }

        private async Task TestForSuccess(
            TokenType tokenType,
            string accountId,
            TransactionFilterParameters filterParams,
            int? expectedRecordCount = null,
            string? scope = null,
            string? xFapiInteractionId = null
            )
        {
            var tokenScope = scope ?? _options.SCOPE;

            //Get token
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType, tokenScope); ;

            //Get claims from token
            Helpers.ExtractClaimsFromToken(accessToken, out var _loginId, out var softwareProductId);

            var encryptedAccountId = Helpers.IdPermanenceEncrypt(accountId, _loginId, softwareProductId);
            var xFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r");

            var baseUrl = $"{_options.DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts/{encryptedAccountId}/transactions";
            var url = GetUrl(baseUrl, filterParams);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetTransactionsAPI(accessToken, xFapiAuthDate: xFapiAuthDate, xv: "1", xFapiInteractionId: xFapiInteractionId, url: url);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                response.Content?.Headers.Should().NotBeNull();
                response.Content?.Headers?.ContentType.Should().NotBeNull();
                response.Content?.Headers?.ContentType?.ToString().Should().Be("application/json; charset=utf-8");

                // Assert - Check XV
                Assertions.AssertHasHeader(api.XV, response.Headers, "x-v");

                // Assert - Check x-fapi-interaction-id
                Assertions.AssertHasHeader(null, response.Headers, "x-fapi-interaction-id");

                // Get expected response
                (var expectedResponse, var totalRecords) = GetExpectedResponse(accountId, accessToken, baseUrl, url, filterParams);

                if (expectedRecordCount.HasValue)
                {
                    totalRecords.Should().Be(expectedRecordCount);
                }

                // Assert - Check json
                await Assertions.AssertHasContentJson(expectedResponse, response.Content);
            }
        }

        private async Task TestForError(TokenType tokenType,
            string accountId,
            CdrException expectedError,
            TransactionFilterParameters filterParams,
            string? xFapiAuthDate = "*NOW*",
            string? xFapiInteractionId = null,
            string? scope = null,
            string? expectedWWWAuthenticate = null,
            bool expectedWWWAuthenticateStartsWith = false,
            bool tokenExpired = false,
            bool useCache = true,
            string xv = "1")
        {
            var tokenScope = scope ?? _options.SCOPE;

            string? accessToken;
            try
            {
                accessToken = tokenExpired ?
                Constants.AccessTokens.ConsumerAccessTokenBankingExpired :
                accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType, tokenScope, useCache);
            }
            catch (AuthoriseException ex)
            {
                // Assert - Check status code
                ex.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check error response
                ex.Error.Should().Be(expectedError.Code);
                ex.ErrorDescription.Should().Be(expectedError.Detail);
                return;
            }

            // we need encrypted account id to make api call
            string encryptedAccountId;
            if (accessToken == null || accessToken == "" || accessToken == "foo")
            {
                encryptedAccountId = Helpers.IdPermanenceEncrypt(accountId, FOO_GUID, FOO_GUID);
            }
            else
            {
                Helpers.ExtractClaimsFromToken(accessToken, out var loginId, out var softwareProductId);
                encryptedAccountId = Helpers.IdPermanenceEncrypt(accountId, loginId, softwareProductId);
            }

            // Arrange
            if (xFapiAuthDate == "*NOW*")
            {
                xFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r");
            }

            var baseUrl = $"{_options.DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts/{encryptedAccountId}/transactions";
            var url = GetUrl(baseUrl, filterParams);

            // Act
            var api = _apiServiceDirector.BuildDataHolderBankingGetTransactionsAPI(accessToken, xFapiAuthDate: xFapiAuthDate, xv: xv, xFapiInteractionId: xFapiInteractionId, url: url);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check ContentType (if response has content)
                if (!string.IsNullOrEmpty(await response.Content.ReadAsStringAsync()))
                {
                    response.Content?.Headers.Should().NotBeNull();
                    response.Content?.Headers?.ContentType.Should().NotBeNull();
                    response.Content?.Headers?.ContentType?.ToString().Should().Be("application/json; charset=utf-8");
                }

                // Assert - Check WWWAutheticate header
                if (response.StatusCode != HttpStatusCode.OK && expectedWWWAuthenticate != null)
                {
                    Assertions.AssertHasHeader(expectedWWWAuthenticate, response.Headers, "WWW-Authenticate", expectedWWWAuthenticateStartsWith);
                }

                //InvalidTokenException doesn't include content, so we don't compare the respnse content
                if (expectedError.GetType() != typeof(InvalidTokenException))
                {
                    // Assert - Check error response content
                    var errorList = new ResponseErrorListV2(expectedError, string.Empty);
                    var expectedContent = JsonConvert.SerializeObject(errorList);
                    await Assertions.AssertHasContentJson<ResponseErrorListV2>(expectedContent, response.Content);
                }
            }
        }
    }
}
