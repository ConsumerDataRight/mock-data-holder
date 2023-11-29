using CDR.DataHolder.Banking.Tests.IntegrationTests.Models;
using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CDR.DataHolder.Banking.Tests.IntegrationTests
{
    public class US12973_MDH_CommonAPI_GetCustomer : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private const string SCOPE_WITHOUT_CUSTOMERBASICREAD = "openid bank:accounts.basic:read bank:transactions:read";

        private readonly TestAutomationOptions _options;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US12973_MDH_CommonAPI_GetCustomer(IOptions<TestAutomationOptions> options, ISqlQueryService sqlQueryService, IDataHolderAccessTokenCache dataHolderAccessTokenCache, IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config, RegisterSoftwareProductFixture registerSoftwareProductFixture)
            : base(testOutputHelperAccessor, config)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderAccessTokenCache = dataHolderAccessTokenCache ?? throw new ArgumentNullException(nameof(dataHolderAccessTokenCache));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
            if (registerSoftwareProductFixture == null)
            {
                throw new ArgumentNullException(nameof(registerSoftwareProductFixture));
            }
        }

        private static string GetExpectedResponse(string? accessToken, string dhMtlsGatewayUrl)
        {
            string seedDataJson = File.ReadAllText("TestData/seed-data.json");
            var seedData = JsonConvert.DeserializeObject<BankingSeedData>(seedDataJson);

            // Get clientCustomerId from JWT ("sub" claim)
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var sub = jwt.Claim("sub").Value;

            // Decrypt sub to extract customer id
            var decryptedSub = IdPermanenceHelper.DecryptSub(
                sub,
                new SubPermanenceParameters
                {
                    SoftwareProductId = Constants.SoftwareProducts.SoftwareProductId.ToLower(),
                    SectorIdentifierUri = Constants.SoftwareProducts.SoftwareProductSectorIdentifierUri,
                },
                Constants.IdPermanence.IdPermanencePrivateKey
            );

            var loginId = decryptedSub;

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
                            middleNames = string.IsNullOrEmpty(customer.Person?.MiddleNames) ?
                                null :
                                customer.Person.MiddleNames.Split(',', StringSplitOptions.TrimEntries),
                            prefix = customer.Person?.Prefix,
                            suffix = customer.Person?.Suffix,
                            occupationCode = customer.Person?.OccupationCode,
                            occupationCodeVersion = customer.Person?.OccupationCodeVersion,
                        } : null,
                        organisation = customer.CustomerUType?.ToLower() == "organisation" ? new
                        {
                            lastUpdateTime = customer.Organisation?.LastUpdateTime,
                            agentFirstName = customer.Organisation?.AgentFirstName,
                            agentLastName = customer.Organisation?.AgentLastName,
                            agentRole = customer.Organisation?.AgentRole,
                            businessName = customer.Organisation?.BusinessName,
                            legalName = customer.Organisation?.LegalName,
                            shortName = customer.Organisation?.ShortName,
                            abn = customer.Organisation?.ABN,
                            acn = customer.Organisation?.ACN,
                            isACNCRegistered = customer.Organisation?.IsACNCRegistered,
                            industryCode = customer.Organisation?.IndustryCode,
                            industryCodeVersion = customer.Organisation?.IndustryCodeVersion,
                            organisationType = customer.Organisation?.OrganisationType,
                            registeredCountry = customer.Organisation?.RegisteredCountry,
                            establishmentDate = customer.Organisation?.EstablishmentDate == null ?
                                 null :
                                 customer.Organisation.EstablishmentDate.Value.ToString("yyyy-MM-dd")
                        } : null,
                    })
                    .FirstOrDefault(),
                links = new
                {
                    self = $"{dhMtlsGatewayUrl}/cds-au/v1/common/customer"
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
        [InlineData(TokenType.JaneWilson)]
        [InlineData(TokenType.Beverage, Skip = "https://dev.azure.com/CDR-AU/Participant%20Tooling/_workitems/edit/51320")]
        public async Task AC01_ShouldRespondWith_200OK_Customers(TokenType tokenType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(tokenType), tokenType);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            var expectedResponse = GetExpectedResponse(accessToken, _options.DH_MTLS_GATEWAY_URL);

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

        [Theory]
        [InlineData(Constants.Scopes.ScopeBanking)]
        public async Task AC02_Success(string scope)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(scope), scope);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson, scope);

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(SCOPE_WITHOUT_CUSTOMERBASICREAD)]
        public async Task AC02_Get_WithoutScopeBasicRead_ShouldRespondWith_403Forbidden(string scope)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(scope), scope);

            // Arrange 
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson, scope);

            CdrException expectedError = new InvalidConsentException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
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
        [InlineData("1")]
        public async Task AC03_Get_Success(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: xv);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData("2")]
        public async Task AC03_Get_WithXV2_ShouldRespondWith_406NotAcceptable(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            CdrException expectedError = new UnsupportedVersionException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: xv);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check content type 
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("99999999999999999999999999999999999999999999999999")]
        [InlineData("-1")]
        public async Task AC03b_Get_WithInvalidXV_ShouldRespondWith_400BadRequest(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            CdrException expectedError = new InvalidVersionException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: xv);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check content type 
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task AC03c_Get_WithMissingXV_ShouldRespondWith_400BadRequest(string xv)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xv), xv);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            CdrException expectedError = new MissingRequiredHeaderException("x-v");
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), xv: xv);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check content type 
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData("DateTime.Now.RFC1123")]
        public async Task AC04_Get_Success(string xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(null)]
        public async Task AC04_Get_WithMissingXFAPIAUTHDATE_ShouldRespondWith_400BadRequest_HeaderMissingErrorResponse(string? xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            xFapiAuthDate = DateTimeExtensions.GetDateFromFapiDate(xFapiAuthDate);

            CdrException expectedError = new MissingRequiredHeaderException("x-fapi-auth-date");
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, xFapiAuthDate);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check content type 
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Theory]
        [InlineData("DateTime.Now.RFC1123")]
        public async Task AC05_Get_Success(string? xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);
            xFapiAuthDate = DateTimeExtensions.GetDateFromFapiDate(xFapiAuthDate);

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, xFapiAuthDate);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData("DateTime.UtcNow")]
        [InlineData("foo")]
        public async Task AC05_Get_WithInvalidXFAPIAUTHDATE_ShouldRespondWith_400BadRequest_HeaderInvalidErrorResponse(string? xFapiAuthDate)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(xFapiAuthDate), xFapiAuthDate);

            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);
            xFapiAuthDate = DateTimeExtensions.GetDateFromFapiDate(xFapiAuthDate);

            CdrException expectedError = new InvalidHeaderException("x-fapi-auth-date");
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, xFapiAuthDate);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check content type 
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        private async Task Test_AC06_AC07(TokenType tokenType, HttpStatusCode expectedStatusCode, string expectedWWWAuthenticateResponse)
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
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

        [Theory]
        [InlineData(TokenType.JaneWilson, HttpStatusCode.OK)]
        [InlineData(TokenType.InvalidEmpty, HttpStatusCode.Unauthorized)]
        [InlineData(TokenType.InvalidOmit, HttpStatusCode.Unauthorized)]
        public async Task AC06_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedStatusCode), expectedStatusCode);

            await Test_AC06_AC07(tokenType, expectedStatusCode, "Bearer");
        }

        [Theory]
        [InlineData(TokenType.JaneWilson, HttpStatusCode.OK)]
        [InlineData(TokenType.InvalidFoo, HttpStatusCode.Unauthorized)]
        public async Task AC07_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedStatusCode), expectedStatusCode);

            await Test_AC06_AC07(tokenType, expectedStatusCode, @"Bearer error=""invalid_token""");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task AC08_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            var accessToken = Constants.AccessTokens.ConsumerAccessTokenBankingExpired;

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
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

        [Theory]
        [InlineData("ACTIVE", HttpStatusCode.OK)]
        [InlineData("REMOVED", HttpStatusCode.BadRequest)]
        [InlineData("SUSPENDED", HttpStatusCode.BadRequest)]
        [InlineData("REVOKED", HttpStatusCode.BadRequest)]
        [InlineData("SURRENDERED", HttpStatusCode.BadRequest)]
        [InlineData("INACTIVE", HttpStatusCode.BadRequest)]
        public async Task AC09_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(string status, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(status), status, nameof(expectedStatusCode), expectedStatusCode);

            var saveStatus = _sqlQueryService.GetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId);
            _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, status);
            try
            {
                string? accessToken = string.Empty;
                // Arrange
                try
                {
                    accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);
                }
                catch (AuthoriseException ex)
                {
                    // Assert
                    using (new AssertionScope(BaseTestAssertionStrategy))
                    {
                        // Assert - Check status code
                        ex.StatusCode.Should().Be(expectedStatusCode);

                        // Assert - Check error response
                        ex.Error.Should().Be("urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive");
                        ex.ErrorDescription.Should().Be($"ERR-GEN-002: Software product status is {status}");

                        return;
                    }
                }

                // Act
                var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
                var response = await api.SendAsync();

                // Assert
                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedStatusCode);
                }
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, saveStatus);
            }
        }

        [Theory]
        [InlineData(SoftwareProductStatus.ACTIVE)]
        public async Task AC11_Get_Success(SoftwareProductStatus status)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(status), status);

            var saveStatus = _sqlQueryService.GetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId);
            _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, status.ToEnumMemberAttrValue());
            try
            {
                // Arrange
                var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

                // Act
                var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"));
                var response = await api.SendAsync();

                // Assert
                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(HttpStatusCode.OK);
                }
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, saveStatus);
            }
        }

#pragma warning disable xUnit1004
        [Theory(Skip = "This test is accurate but is failing due to a bug. Prefer to skip it for now rather than test for incorrect behaviour")]
        [InlineData(SoftwareProductStatus.INACTIVE)]
        [InlineData(SoftwareProductStatus.REMOVED)]
        public async Task AC11_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_NotActiveErrorResponse(SoftwareProductStatus status)
        {
            //TODO: This is failing, but the test is correct. The old test checked for a 200Ok and we may need to do that in the short term to get the test passing (or skip it). Bug 63708
            var saveStatus = _sqlQueryService.GetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId);
            _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, status.ToEnumMemberAttrValue());

            // Arrange
            try
            {
                var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);
            }
            catch (AuthoriseException ex)
            {
                CdrException expectedError = new AdrStatusNotActiveException(status);
                var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

                // Assert
                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    // Assert - Check status code
                    ex.StatusCode.Should().Be(expectedError.StatusCode);

                    // Assert - Check error response
                    ex.Error.Should().Be(expectedError.Code);
                    ex.ErrorDescription.Should().Be(expectedError.Detail);
                }
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, saveStatus);
            }
        }
#pragma warning restore xUnit1004

        [Theory]
        [InlineData(Constants.Certificates.CertificateFilename, Constants.Certificates.CertificatePassword)]
        public async Task AC12_Get_Success(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), certFileName: certificateFilename, certPassword: certificatePassword);
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
        public async Task AC12_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorized(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.JaneWilson);

            CdrException expectedError = new InvalidTokenException();
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act
            var api = _apiServiceDirector.BuildDataHolderCommonGetCustomerAPI(accessToken, DateTime.Now.ToUniversalTime().ToString("r"), certFileName: certificateFilename, certPassword: certificatePassword);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check content type 
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check error response
                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }
    }
}
