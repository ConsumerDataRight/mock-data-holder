using CDR.DataHolder.Energy.Tests.IntegrationTests.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.APIs;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Dataholders.Energy;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Jose;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests
{
    public class US27571_MDH_EnergyAPI_GetAccounts : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        const string SCOPE_ACCOUNTS_BASIC_READ = "openid profile common:customer.basic:read energy:accounts.basic:read";
        const string SCOPE_WITHOUT_ACCOUNTS_BASIC_READ = "openid profile common:customer.basic:read";

        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly IApiServiceDirector _apiServiceDirector;
        private readonly TestAutomationOptions _options;

        private readonly string ENERGY_GET_ACCOUNTS_BASE_URL;

        public US27571_MDH_EnergyAPI_GetAccounts(
            IOptions<TestAutomationOptions> options,
            ISqlQueryService sqlQueryService,
           IDataHolderParService dataHolderParService,
           IDataHolderTokenService dataHolderTokenService,
           IDataHolderAccessTokenCache dataHolderAccessTokenCache,
           IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config,
            RegisterSoftwareProductFixture registerSoftwareProductFixture)
            : base(testOutputHelperAccessor, config)
        {
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _dataHolderAccessTokenCache = dataHolderAccessTokenCache ?? throw new ArgumentNullException(nameof(dataHolderAccessTokenCache));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (registerSoftwareProductFixture == null)
            {
                throw new ArgumentNullException(nameof(registerSoftwareProductFixture));
            }

            ENERGY_GET_ACCOUNTS_BASE_URL = $"{_options.DH_MTLS_GATEWAY_URL}/cds-au/v1/energy/accounts";
        }

        [Theory]
        [InlineData(TokenType.MaryMoss, "1")]
        public async Task AC01_GetAccounts_ShouldRespondWith_200OK_Accounts(TokenType tokenType, string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(apiVersion), apiVersion);

            await Test_ValidGetAccountsScenario(tokenType, apiVersion: apiVersion);
        }

        // Note: Covers US45029-AC01a, US45029-AC01b, US45029-AC01c and US45029-AC01d
        [Theory]
        [InlineData(TokenType.MaryMoss, "ALL", "2")]
        [InlineData(TokenType.MaryMoss, "OPEN", "2")]
        [InlineData(TokenType.MaryMoss, "CLOSED", "2")]
        [InlineData(TokenType.MaryMoss, null, "2")]
        public async Task AC01_GetAccountsV2_ShouldRespondWith_200OK_Accounts(TokenType tokenType, string? openStatus, string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(tokenType), tokenType, nameof(openStatus), openStatus, nameof(apiVersion), apiVersion);

            await Test_ValidGetAccountsScenario(tokenType, apiVersion: apiVersion, openStatus: openStatus);
        }

        [Theory]
        [InlineData("DateTime.Now.RFC1123", "1")]
        [InlineData("DateTime.Now.RFC1123", "2")]
        public async Task AC02_AC09_Get_WithValidXFAPIAuthDate_Success(
          string xFapiAuthDate,
          string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(xFapiAuthDate), xFapiAuthDate, nameof(apiVersion), apiVersion);
            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTimeExtensions.GetDateFromFapiDate(xFapiAuthDate), xv: apiVersion);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check application/json
                Assertions.AssertHasContentTypeApplicationJson(response.Content);
            }
        }

        [Theory]
        [InlineData("000", "1", CdsError.InvalidHeader)]
        [InlineData("foo", "1", CdsError.InvalidHeader)]
        [InlineData("", "1", CdsError.InvalidHeader)]
        [InlineData(null, "1", CdsError.MissingRequiredHeader)]
        [InlineData("DateTime.UtcNow", "1", CdsError.InvalidHeader)]
        [InlineData("000", "2", CdsError.InvalidHeader)]
        [InlineData("foo", "2", CdsError.InvalidHeader)]
        [InlineData("", "2", CdsError.InvalidHeader)]
        [InlineData(null, "2", CdsError.MissingRequiredHeader)]
        [InlineData("DateTime.UtcNow", "2", CdsError.InvalidHeader)]
        public async Task AC02_AC09_Get_WithInvalidXFAPIAuthDate_ShouldRespondWith_400BadRequest(
            string xFapiAuthDate,
            string apiVersion,
            CdsError cdsError)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(xFapiAuthDate), xFapiAuthDate, nameof(apiVersion), apiVersion, nameof(cdsError), cdsError);
            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            CdrException expectedError;
            if (cdsError == CdsError.MissingRequiredHeader)
            {
                expectedError = new MissingRequiredHeaderException($"x-fapi-auth-date");
            }
            else if (cdsError == CdsError.InvalidHeader)
            {
                expectedError = new InvalidHeaderException($"x-fapi-auth-date");
            }
            else
            {
                throw new InvalidOperationException($"The CdsError parameter is not handled within this test case: {cdsError}").Log();
            }

            var errorList = new ResponseErrorListV2(expectedError.Code, expectedError.Title, expectedError.Detail, null);
            var expectedContent = JsonConvert.SerializeObject(errorList);

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTimeExtensions.GetDateFromFapiDate(xFapiAuthDate), xv: apiVersion);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check application/json
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData(null, 1001, CdsError.InvalidPageSize, "1")]
        [InlineData(100, null, CdsError.InvalidPage, "1")]
        [InlineData(0, null, CdsError.InvalidField, "1")]
        [InlineData(null, 1001, CdsError.InvalidPageSize, "2")]
        [InlineData(100, null, CdsError.InvalidPage, "2")]
        [InlineData(0, null, CdsError.InvalidField, "2")]
        public async Task AC03_AC06_AC07_Get_WithInvalidPageSizeOrInvalidPageOrInvalidField(int? page, int? pageSize, CdsError cdsError, string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}, {P4}={V4}.", nameof(page), page, nameof(pageSize), pageSize, nameof(cdsError), cdsError, nameof(apiVersion), apiVersion);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            var baseUrl = ENERGY_GET_ACCOUNTS_BASE_URL;
            var url = GetUrl(baseUrl, queryPageSize: pageSize, queryPage: page);

            CdrException expectedError = cdsError switch
            {
                CdsError.InvalidPageSize => new InvalidPageSizeException("page-size pagination field is greater than the maximum 1000 allowed"),
                CdsError.InvalidPage => new InvalidPageException("Page parameter is out of range.  Maximum page is 1"),
                CdsError.InvalidField => new InvalidFieldException("Page parameter is out of range. Minimum page is 1, maximum page is 1000"),
                _ => throw new InvalidOperationException($"The CdsError parameter is not handled within this test case: {cdsError}").Log()
            };

            var errorList = new ResponseErrorListV2(expectedError, string.Empty);
            var expectedContent = JsonConvert.SerializeObject(errorList);

            // Act
            var response = await GetAccounts(accessToken, url, apiVersion: apiVersion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check application/json
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData("foo", CdsError.InvalidVersion)]
        [InlineData("-1", CdsError.InvalidVersion)]
        [InlineData("3", CdsError.UnsupportedVersion)]
        [InlineData("", CdsError.InvalidVersion)]
        [InlineData(null, CdsError.MissingRequiredHeader)]
        public async Task AC04_AC05_AC08_Get_WithInvalidXV_Failure(
            string apiVersion,
            CdsError cdsError)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(apiVersion), apiVersion, nameof(cdsError), cdsError);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            CdrException expectedError;
            if (cdsError == CdsError.InvalidVersion)
            {
                expectedError = new InvalidVersionException();
            }
            else if (cdsError == CdsError.UnsupportedVersion)
            {
                expectedError = new UnsupportedVersionException();
            }
            else if (cdsError == CdsError.MissingRequiredHeader)
            {
                expectedError = new MissingRequiredHeaderException("An API version x-v header is required, but was not specified.");
            }
            else
            {
                throw new InvalidOperationException($"The CdsError parameter is not handled within this test case: {cdsError}").Log();
            }

            var errorList = new ResponseErrorListV2(expectedError.Code, expectedError.Title, expectedError.Detail, null);
            var expectedContent = JsonConvert.SerializeObject(errorList);

            // Act
            var response = await GetAccounts(accessToken, ENERGY_GET_ACCOUNTS_BASE_URL, apiVersion: apiVersion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check application/json
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task ACX01_Get_WithPageSize5_ShouldRespondWith_200OK_Page1Of5Records()
        {
            await Test_ValidGetAccountsScenario(TokenType.MaryMoss, queryPage: 1, queryPageSize: 5);
        }

        [Fact]
        public async Task ACX02_Get_WithPageSize5_AndPage3_ShouldRespondWith_200OK_Page3Of5Records()
        {
            await Test_ValidGetAccountsScenario(TokenType.MaryMoss, queryPage: 3, queryPageSize: 5);
        }

        [Fact]
        public async Task ACX03_Get_WithPageSize5_AndPage5_ShouldRespondWith_200OK_Page5Of5Records()
        {
            await Test_ValidGetAccountsScenario(TokenType.MaryMoss, queryPage: 5, queryPageSize: 5);
        }

        [Theory]
        [InlineData(SCOPE_ACCOUNTS_BASIC_READ, "1")]
        [InlineData(SCOPE_ACCOUNTS_BASIC_READ, "2")]
        public async Task ACX04_Get_Success(string scope, string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(scope), scope, nameof(apiVersion), apiVersion);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss, scope);

            // Act
            var response = await GetAccounts(accessToken, ENERGY_GET_ACCOUNTS_BASE_URL, apiVersion: apiVersion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(SCOPE_WITHOUT_ACCOUNTS_BASIC_READ, "1")]
        [InlineData(SCOPE_WITHOUT_ACCOUNTS_BASIC_READ, "2")]
        public async Task ACX04_Get_WithoutEnergyAccountsReadScope_ShouldRespondWith_403Forbidden(string scope, string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(scope), scope, nameof(apiVersion), apiVersion);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss, scope);

            CdrException expectedError = new InvalidConsentException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError, string.Empty));

            // Act
            var response = await GetAccounts(accessToken, ENERGY_GET_ACCOUNTS_BASE_URL, apiVersion: apiVersion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check application/json
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData(TokenType.MaryMoss, HttpStatusCode.OK, "1")]
        [InlineData(TokenType.InvalidFoo, HttpStatusCode.Unauthorized, "1")]
        [InlineData(TokenType.MaryMoss, HttpStatusCode.OK, "2")]
        [InlineData(TokenType.InvalidFoo, HttpStatusCode.Unauthorized, "2")]
        public async Task ACX05_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorized(TokenType tokenType, HttpStatusCode expectedStatusCode, string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(tokenType), tokenType, nameof(expectedStatusCode), expectedStatusCode, nameof(apiVersion), apiVersion);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            // Act
            var response = await GetAccounts(accessToken, ENERGY_GET_ACCOUNTS_BASE_URL, apiVersion: apiVersion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);
            }
        }

        [Theory]
        [InlineData(TokenType.MaryMoss, HttpStatusCode.OK)]
        [InlineData(TokenType.InvalidEmpty, HttpStatusCode.Unauthorized)]
        [InlineData(TokenType.InvalidOmit, HttpStatusCode.Unauthorized)]
        public async Task ACX06_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorized(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            // Act
            var response = await GetAccounts(accessToken, ENERGY_GET_ACCOUNTS_BASE_URL);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check error response 
                    Assertions.AssertHasHeader("Bearer", response.Headers, "WWW-Authenticate");
                }
            }

        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task ACX07_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized(HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            var accessToken = Constants.AccessTokens.ConsumerAccessTokenEnergyExpired;

            // Act
            var response = await GetAccounts(accessToken, ENERGY_GET_ACCOUNTS_BASE_URL);

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

        [Theory]
        [InlineData("123", HttpStatusCode.OK, "1")]
        [InlineData("123", HttpStatusCode.OK, "2")]
        public async Task ACX10_Get_WithXFAPIInteractionId123_ShouldRespondWith_200OK_Accounts_AndXFapiInteractionIDis123(string xFapiInteractionId, HttpStatusCode expectedStatusCode, string apiVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(xFapiInteractionId), xFapiInteractionId, nameof(expectedStatusCode), expectedStatusCode, nameof(apiVersion), apiVersion);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: apiVersion, xFapiInteractionId: xFapiInteractionId);
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
        public async Task ACX11_Get_Success(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), certFileName: certificateFilename, certPassword: certificatePassword);
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
        public async Task ACX11_Get_WithDifferentHolderOfKey_401Unauthorized(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            CdrException expectedError = new InvalidTokenException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError, string.Empty));

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), certFileName: certificateFilename, certPassword: certificatePassword);
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
        [InlineData(Constants.Users.Energy.UserIdMaryMoss, Constants.Accounts.Energy.AccountIdsAllMaryMoss)] // All accounts
        [InlineData(Constants.Users.Energy.UserIdMaryMoss, Constants.Accounts.Energy.AccountIdsSubsetMaryMoss)] // Subset of accounts
        public async Task ACX12_Get_WhenConsumerDidNotGrantConsentToAllAccounts_ShouldRespondWith_200OK_ConsentedAccounts(string userId, string consentedAccounts)
        {
            async Task<ResponseEnergyAccountListV2?> GetAccounts2(string? accessToken)
            {
                var response = await GetAccounts(accessToken, ENERGY_GET_ACCOUNTS_BASE_URL);

                if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Error getting accounts").Log();

                var json = await response.Content.ReadAsStringAsync();

                var accountsResponse = JsonConvert.DeserializeObject<ResponseEnergyAccountListV2>(json);

                return accountsResponse;
            }

            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(userId), userId, nameof(consentedAccounts), consentedAccounts);

            // Arrange - Get authcode
            var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector)
          .WithUserId(userId)
          .WithSelectedAccountIds(consentedAccounts)
          .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Act
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);
            var accountsResponse = await GetAccounts2(tokenResponse?.AccessToken);
            Helpers.ExtractClaimsFromToken(tokenResponse?.AccessToken, out var custId, out var softwareProductId);
            var encryptedAccountIds = consentedAccounts.Split(',').Select(consentedAccountId => Helpers.IdPermanenceEncrypt(consentedAccountId, custId, softwareProductId));

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check each account in response is one of the consented accounts
                foreach (var account in accountsResponse?.Data?.Accounts ?? throw new NullReferenceException("Response contains not accounts data.").Log())
                {
                    encryptedAccountIds.Should().Contain(account.AccountId);
                }
            }
        }

        [Theory]
        [InlineData(Constants.Users.Energy.UserIdMaryMoss, Constants.Accounts.Energy.AccountIdsAllMaryMoss)]
        public async Task ACX13_GetAccountsMultipleTimes_ShouldRespondWith_SameEncryptedAccountIds(string userId, string consentedAccounts)
        {
            async Task<string?[]?> GetAccountIds(string userId, string consentedAccounts)
            {
                async Task<ResponseEnergyAccountListV2?> GetAccounts(string? accessToken)
                {
                    var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
                    var response = await api.SendAsync();

                    if (response.StatusCode != HttpStatusCode.OK) throw new Exception("Error getting accounts").Log();

                    var json = await response.Content.ReadAsStringAsync();

                    var accountsResponse = JsonConvert.DeserializeObject<ResponseEnergyAccountListV2>(json);

                    return accountsResponse;
                }

                // Get authcode
                var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector)
                    .WithUserId(userId)
                    .WithSelectedAccountIds(consentedAccounts)
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

        [Theory]
        [InlineData("1", "1", "1")]  //Valid. Should return v1
        [InlineData("1", "2", "1")]  //Valid. Should return v1 - x-min-v is ignored when > x-v
        [InlineData("2", "1", "2")]  //Valid. Should return v2 - x-v is supported and higher than x-min-v 
        [InlineData("2", "2", "2")]  //Valid. Should return v2 - x-v is supported equal to x-min-v        
        [InlineData("3", "2", "2")]  //Valid. Should return v2 - x-v is NOT supported and x-min-v is supported
        [InlineData("2", "3", "2")]  //Valid. Should return v2 - x-min-v is ignored when > x-v (test using highest supported version)
        public async Task ACX14_ApiVersionAndMinimumSupportedVersionScenarios_Success(
            string apiVersion,
            string apiMinVersion,
            string expectedApiVersionResponse)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(apiVersion), apiVersion, nameof(apiMinVersion), apiMinVersion, nameof(expectedApiVersionResponse), expectedApiVersionResponse);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: apiVersion, xMinV: apiMinVersion);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check XV
                Assertions.AssertHasHeader(expectedApiVersionResponse, response.Headers, "x-v");
            }
        }

        [Theory]
        [InlineData("3", "3")] //Invalid. Both x-v and x-min-v exceed MDHE supported version of 2
        public async Task ACX14_ApiVersionAndMinimumSupportedVersionScenarios_406NotAcceptable(
           string apiVersion,
           string apiMinVersion)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(apiVersion), apiVersion, nameof(apiMinVersion), apiMinVersion);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            CdrException expectedError = new UnsupportedVersionException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError, string.Empty));

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: apiVersion, xMinV: apiMinVersion);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check application/json
                Assertions.AssertHasContentTypeApplicationJson(response.Content);
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        private async Task<HttpResponseMessage> GetAccounts(string? accessToken, string url, string? apiVersion = "1")
        {
            var api = _apiServiceDirector.BuildDataHolderEnergyGetAccountsAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: apiVersion, url: url);
            return await api.SendAsync();
        }

        private async Task Test_ValidGetAccountsScenario(
           TokenType tokenType,
           bool? isOwned = null, string? openStatus = null, string? productCategory = null,
           int? queryPage = null, int? queryPageSize = null,
           int? expectedRecordCount = null, string apiVersion = "1")
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType, scope: SCOPE_ACCOUNTS_BASIC_READ);

            var baseUrl = ENERGY_GET_ACCOUNTS_BASE_URL;
            var url = GetUrl(baseUrl, isOwned, openStatus, productCategory, queryPage, queryPageSize);

            (var expectedResponse, var totalRecords) = GetExpectedResponse(accessToken, baseUrl, url,
                isOwned, openStatus, productCategory,
                queryPage, queryPageSize, apiVersion: apiVersion);

            // Act
            var response = await GetAccounts(accessToken, url, apiVersion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check XV
                Assertions.AssertHasHeader(apiVersion.ToString(), response.Headers, "x-v");

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

        private static string? ToStringOrNull(DateTime? dateTime)
        {
            return dateTime?.ToString("yyyy-MM-dd");
        }

        private static (string, int) GetExpectedResponse(string? accessToken,
            string baseUrl, string selfUrl,
            bool? isOwned = null, string? openStatus = null, string? productCategory = null,
            int? page = null, int? pageSize = null, string? apiVersion = null)
        {
            Helpers.ExtractClaimsFromToken(accessToken, out var loginId, out var softwareProductId);

            var effectivePage = page ?? 1;
            var effectivePageSize = pageSize ?? 25;

            string seedDataJson = File.ReadAllText("TestData/seed-data.json");
            var seedData = JsonConvert.DeserializeObject<EnergySeedData>(seedDataJson);

            var currentCustomer = seedData?.Customers.Where(c => c.LoginId == loginId).FirstOrDefault();

            var accounts = currentCustomer?.Accounts?
                .Where(account => account.OpenStatus == openStatus || (String.IsNullOrEmpty(openStatus) || openStatus.Equals("ALL", StringComparison.OrdinalIgnoreCase)))
                .Select(account => new
                {
                    accountId = Helpers.IdPermanenceEncrypt(account.AccountId, loginId, softwareProductId),
                    accountNumber = account.AccountNumber,
                    displayName = account.DisplayName,
                    creationDate = account.CreationDate.ToString("yyyy-MM-dd"),
                    openStatus = (apiVersion == "2") ? account.OpenStatus : null,
                    plans = account.AccountPlans.OrderBy(ap => ap.AccountPlanId).Select(accountPlan => new
                    {
                        nickname = accountPlan.Nickname,
                        servicePointIds = accountPlan.ServicePoints.OrderBy(sp => sp.ServicePointId).Select(sp => sp.ServicePointId).ToArray(),
                        planOverview = new
                        {
                            displayName = accountPlan.PlanOverview.DisplayName,
                            startDate = accountPlan.PlanOverview.StartDate.ToString("yyyy-MM-dd"),
                            endDate = ToStringOrNull(accountPlan.PlanOverview.EndDate)
                        }
                    }),
                })
                .ToList();


            var totalRecords = accounts == null ? 0 : accounts.Count;

            // Paging
            accounts = accounts?
                .OrderBy(account => account.displayName).ThenBy(account => account.accountId)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);

            const int MINPAGE = 1;
            if (page < MINPAGE)
            {
                throw new Exception($"Page {page} out of range. Min Page is {MINPAGE}").Log();
            }
            var maxPage = ((totalRecords - 1) / pageSize) + 1;
            if (page > maxPage)
            {
                throw new Exception($"Page {page} out of range. Max Page is {maxPage} (Records={totalRecords}, PageSize={pageSize})").Log();
            }

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
                }),

                totalRecords
            );
        }

        private static string GetUrl(string baseUrl,
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

    }
}
