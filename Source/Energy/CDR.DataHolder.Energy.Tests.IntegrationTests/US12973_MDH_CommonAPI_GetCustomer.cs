using System.Net;
using CDR.DataHolder.Energy.Tests.IntegrationTests.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests
{
    public class US12973_Mdh_CommonApi_GetCustomer : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly IApiServiceDirector _apiServiceDirector;
        private readonly TestAutomationOptions _options;

        public US12973_Mdh_CommonApi_GetCustomer(
            IOptions<TestAutomationOptions> options,
            IDataHolderAccessTokenCache dataHolderAccessTokenCache,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config,
            RegisterSoftwareProductFixture registerSoftwareProductFixture)
            : base(testOutputHelperAccessor, config)
        {
            _dataHolderAccessTokenCache = dataHolderAccessTokenCache ?? throw new ArgumentNullException(nameof(dataHolderAccessTokenCache));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (registerSoftwareProductFixture == null)
            {
                throw new ArgumentNullException(nameof(registerSoftwareProductFixture));
            }
        }

        [Theory]
        [InlineData(TokenType.MaryMoss)]
        [InlineData(TokenType.HeddaHare)]
        public async Task AC01_ShouldRespondWith_200OK_Customers(TokenType tokenType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(tokenType), tokenType);

            // Arrange
            string? accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            accessToken = accessToken ?? throw new InvalidOperationException($"{nameof(accessToken)} is null.");

            var expectedResponse = GetExpectedResponse(_options.DH_MTLS_GATEWAY_URL, tokenType.GetUserIdByTokenType() ?? throw new InvalidOperationException($"{nameof(tokenType)} is null."));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
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
            }
        }

        private static string GetExpectedResponse(string dhMtlsGatewayUrl, string loginId)
        {
            string seedDataJson = File.ReadAllText("TestData/seed-data.json");
            var seedData = JsonConvert.DeserializeObject<EnergySeedData>(seedDataJson);

            // Get expected response
            var expectedResponse = new
            {
                data = seedData?.Customers
                    .Where(customer => customer.LoginId == loginId)
                    .Select(customer => new
                    {
                        customerUType = customer.CustomerUType,
                        person = customer.CustomerUType?.ToLower() == "person" ? new
                        {
                            lastUpdateTime = customer.Person?.LastUpdateTime,
                            firstName = customer.Person?.FirstName,
                            lastName = customer.Person?.LastName,
                            middleNames = GetMiddleNames(customer.Person?.MiddleNames),
                            prefix = customer.Person?.Prefix,
                            suffix = customer.Person?.Suffix,
                            occupationCode = customer.Person?.OccupationCode,
                            occupationCodeVersion = customer.Person?.OccupationCodeVersion
                        }
                        : null,
                    })
                    .FirstOrDefault(),
                links = new
                {
                    self = $"{dhMtlsGatewayUrl}/cds-au/v1/common/customer"
                },
                meta = new { }
            };

            return JsonConvert.SerializeObject(
                expectedResponse,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                });
        }

        private static string[] GetMiddleNames(string? middleNamesString)
        {
            if (string.IsNullOrEmpty(middleNamesString))
            {
                return [];
            }

            return middleNamesString.Split(',', StringSplitOptions.TrimEntries);
        }
    }
}
