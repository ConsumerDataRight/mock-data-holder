using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests
{
    public class Usx00001_Mdh_InfosecProfileApi_Par_Fapi : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly IDataHolderParService _dataHolderParService;
        private readonly TestAutomationOptions _options;

        public Usx00001_Mdh_InfosecProfileApi_Par_Fapi(
            IOptions<TestAutomationOptions> options,
            IDataHolderParService dataHolderParService,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config,
            RegisterSoftwareProductFixture registerSoftwareProductFixture)
            : base(testOutputHelperAccessor, config)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            if (registerSoftwareProductFixture == null)
            {
                throw new ArgumentNullException(nameof(registerSoftwareProductFixture));
            }
        }

        [Fact]
        public async Task ACX01_FAPI_AudienceAsParUri_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var aud = _options.DH_TLS_AUTHSERVER_BASE_URL + "/connect/par";

            var expectedError = new InvalidAudienceException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: Constants.Scopes.ScopeEnergy, aud: aud);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX02_FAPI_InvalidAudience_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var aud = "https://invalid.issuer";

            var expectedError = new InvalidAudienceException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, aud: aud);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX03_FAPI_NoNbfClaim_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new MissingNbfClaimException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, addNotBeforeClaim: false);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX04_FAPI_NoExpClaim_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new TokenValidationRequestException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, addExpiryClaim: false);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX05_FAPI_ExpiredRequestObject_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new ExpiredRequestException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, nbfOffsetSeconds: -3600, expOffsetSeconds: -3600);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX06_FAPI_NbfGreaterThan60Mins_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new InvalidExpClaimException(); // invalid because we moved the nbf earlier

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, nbfOffsetSeconds: -3600);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX07_FAPI_ExpGreaterThan60Mins_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new InvalidExpClaimException(); // invalid because we moved the exp later

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, expOffsetSeconds: 3600);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX08_FAPI_NoRequestObject_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new MissingRequiredFieldException($"request");
            var errorList = new ResponseErrorListV2(expectedError.Code, expectedError.Title, expectedError.Detail, null!);
            var expectedContent = JsonConvert.SerializeObject(errorList);

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, addRequestObject: false);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedError.StatusCode);

                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task ACX09_FAPI_WithRequestUri_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new UnsupportedRequestUriFormParameterException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, requestUri: Guid.NewGuid().ToString());

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX10_FAPI_WithInvalidRedirectUri_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new InvalidRedirectUriForClientException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, redirectUri: "https://junk.com/invalid");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task ACX11_FAPI_WithResponseModeQuery_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new UnsupportedResponseModeException();

            // Act
            var response = await _dataHolderParService.SendRequest(scope: _options.SCOPE, responseMode: ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums.ResponseMode.Query);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact(Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51303")]
        public async Task ACX12_FAPI_AuthorizeWithNoRequestOrRequestUri_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var url = $"{_options.DH_TLS_AUTHSERVER_BASE_URL}/connect/authorize?client_id=3e6c5f3d-bd58-4aaa-8c23-acfec837b506&redirect_uri=https://www.certification.openid.net/test/a/cdr-mdh/callback&scope=openid%20profile%20common:customer.basic:read%20energy:accounts.basic:read%20energy:accounts.concessions:read%20cdr:registration&claims=%7B%22id_token%22:%7B%22acr%22:%7B%22value%22:%22urn:cds.au:cdr:2%22,%22essential%22:true%7D%7D,%22sharing_duration%22:7776000%7D&state=XhnoTrPAlD&nonce=IkUxjBqVuJ&response_type=code%20id_token";
            using var client = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Helpers.Web.CreateHttpClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                response?.Content?.Headers?.ContentType?.ToString().Should().StartWith("text/html");

                response?.RequestMessage?.RequestUri?.PathAndQuery.Should().StartWith("/connect/authorize");

                var expectedErrorResponse = "ERR-AUTH-008: request_uri is missing";
                var actualResponseContent = response?.Content != null ? await response.Content.ReadAsStringAsync() : null;
                actualResponseContent.Should().Contain(expectedErrorResponse);
            }
        }
    }
}
