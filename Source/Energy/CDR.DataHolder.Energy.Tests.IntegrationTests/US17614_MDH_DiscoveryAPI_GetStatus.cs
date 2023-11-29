using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests
{
    public class US17614_MDH_DiscoveryAPI_GetStatus : BaseTest, IClassFixture<BaseFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US17614_MDH_DiscoveryAPI_GetStatus(
            IOptions<TestAutomationOptions> options,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config,
            BaseFixture baseFixture)
            : base(testOutputHelperAccessor, config)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _apiServiceDirector = apiServiceDirector ?? throw new System.ArgumentNullException(nameof(apiServiceDirector));
            if (baseFixture == null)
            {
                throw new ArgumentNullException(nameof(baseFixture));
            }
        }


        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_Status()
        {
            // Arrange
            var api = _apiServiceDirector.BuildDataHolderDiscoveryStatusAPI();

            // Act
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check json
                var expectedResponse = @$"{{
                    ""data"": {{
                        ""status"": ""OK"",
                        ""explanation"": """",
                        ""detectionTime"": """",
                        ""expectedResolutionTime"": """",
                        ""updateTime"": """"
                    }},
                    ""links"": {{
                        ""self"": ""{_options.MDH_HOST}:8100/cds-au/v1/discovery/status""
                    }},
                    ""meta"": {{}}
                }}";
                await Assertions.AssertHasContentJson(expectedResponse, response.Content);
            }
        }
    }
}
