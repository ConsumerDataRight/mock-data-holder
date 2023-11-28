using CDR.DataHolder.Energy.Tests.IntegrationTests.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests
{
    public class US28722_MDH_EnergyAPI_GetConcessions : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        const string SCOPE_ACCOUNTS_CONCESSIONS_READ = "openid energy:accounts.concessions:read";

        private readonly TestAutomationOptions _options;
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US28722_MDH_EnergyAPI_GetConcessions(
            IOptions<TestAutomationOptions> options,
            IDataHolderAccessTokenCache dataHolderAccessTokenCache,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config,
            RegisterSoftwareProductFixture registerSoftwareProductFixture)
            : base(testOutputHelperAccessor, config)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _dataHolderAccessTokenCache = dataHolderAccessTokenCache ?? throw new ArgumentNullException(nameof(dataHolderAccessTokenCache));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
            if (registerSoftwareProductFixture == null)
            {
                throw new ArgumentNullException(nameof(registerSoftwareProductFixture));
            }
        }

        private static (string, int) GetExpectedResponse(
            string? accessToken,
            string accountId,
            string selfUrl,
            int? page = null,
            int? pageSize = null)
        {
            Helpers.ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);

            var effectivePage = page ?? 1;
            var effectivePageSize = pageSize ?? 25;

            string seedDataJson = File.ReadAllText("TestData/seed-data.json");
            var seedData = JsonConvert.DeserializeObject<EnergySeedData>(seedDataJson);

            var currentCustomer = seedData?.Customers.Where(c => c.LoginId == customerId).FirstOrDefault();

            var currentAccount = currentCustomer?.Accounts?.Where(account => account.AccountId == accountId).FirstOrDefault();

            var concessions = currentAccount?.AccountConcessions
                .Select(accountConcession => new
                {
                    type = accountConcession.Type,
                    displayName = accountConcession.DisplayName,
                    startDate = (accountConcession.StartDate ?? DateTime.MinValue).ToString("yyyy-MM-dd") ?? "",
                    endDate = (accountConcession.EndDate ?? DateTime.MinValue).ToString("yyyy-MM-dd") ?? "",
                    discountFrequency = accountConcession.DiscountFrequency,
                    amount = accountConcession.Amount,
                    percentage = accountConcession.Percentage,
                    appliedTo = (accountConcession.AppliedTo ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries),
                })
                .ToList();

            int totalRecords = concessions == null ? 0 : concessions.Count;

            // Paging
            concessions = concessions?
                .OrderBy(accountConcession => accountConcession.displayName)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecords / effectivePageSize);

            var expectedResponse = new
            {
                data = new
                {
                    concessions,
                },
                links = new
                {
                    self = selfUrl,
                },
                meta = new
                {
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

        static string GetUrl(string baseUrl, int? queryPage = null, int? queryPageSize = null)
        {
            var query = new KeyValuePairBuilder();

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

        private async Task Test_AC01(
           TokenType tokenType,
           string accountId,
           int? queryPage = null, int? queryPageSize = null,
           int? expectedRecordCount = null)
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType, scope: SCOPE_ACCOUNTS_CONCESSIONS_READ);
            Helpers.ExtractClaimsFromToken(accessToken, out var customerId, out var softwareProductId);

            var encryptedAccountId = Helpers.IdPermanenceEncrypt(accountId, customerId, softwareProductId);
            var baseUrl = $"{_options.DH_MTLS_GATEWAY_URL}/cds-au/v1/energy/accounts/{encryptedAccountId}/concessions";
            var url = GetUrl(baseUrl, queryPage, queryPageSize);

            (var expectedResponse, var totalRecords) = GetExpectedResponse(accessToken,
                accountId,
                url,
                queryPage, queryPageSize);

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetConcessionsAPI(accessToken!, DateTime.Now.ToUniversalTime().ToString("r"), url: url);
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

        [Theory]
        [InlineData(TokenType.MaryMoss, Constants.Accounts.Energy.AccountIdMaryMoss)]
        public async Task AC01_Get_ShouldRespondWith_200OK_Concessions(TokenType tokenType, string accountId)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(accountId), accountId);

            await Test_AC01(tokenType, accountId);
        }

        [Theory]
        [InlineData("000")]
        [InlineData("foo")]
        [InlineData("")]
        public async Task AC02_Get_WithInvalidXFAPIAuthDate_ShouldRespondWith_400BadRequest(string xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);
            var accountId = Constants.Accounts.Energy.AccountIdMaryMoss;

            CdrException expectedError = new InvalidHeaderException("x-fapi-auth-date");
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetConcessionsAPI(accessToken!, xFapiAuthDate, encryptedAccountId: accountId);
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
        [InlineData("foo")]
        public async Task AC03_Get_WithInvalidXV_ShouldRespondWith_400BadRequest(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);
            var accountId = Constants.Accounts.Energy.AccountIdMaryMoss;

            CdrException expectedError = new InvalidVersionException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetConcessionsAPI(accessToken!, DateTime.Now.ToUniversalTime().ToString("r"), encryptedAccountId: accountId, xv: xv);
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
        [InlineData("2")]
        public async Task AC04_Get_WithUnsupportedXV_ShouldRespondWith_406NotAcceptable(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);
            var accountId = Constants.Accounts.Energy.AccountIdMaryMoss;

            CdrException expectedError = new UnsupportedVersionException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetConcessionsAPI(accessToken!, DateTime.Now.ToUniversalTime().ToString("r"), encryptedAccountId: accountId, xv: xv);
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
        [InlineData("foo")]
        public async Task AC06_Get_WithInvalidAccountId_ShouldRespondWith_404NotFound(string accountId)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(accountId), accountId);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.MaryMoss);

            CdrException expectedError = new InvalidEnergyAccountException(accountId);
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderEnergyGetConcessionsAPI(accessToken!, DateTime.Now.ToUniversalTime().ToString("r"), encryptedAccountId: accountId);
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
    }
}
