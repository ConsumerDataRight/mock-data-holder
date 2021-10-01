using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US12968_MDH_InfosecProfileAPI_PAR : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        // static private async Task Arrange()
        // {
        //     TestSetup.Register_PatchRedirectUri();
        //     TestSetup.DataHolder_PurgeIdentityServer();
        //     await TestSetup.DataHolder_RegisterSoftwareProduct();
        // }

        [Fact]
        // Call PAR endpoint, with request, to get a RequestUri
        public async Task AC01_Post_ShouldRespondWith_201Created_RequestUri()
        {
            // Arrange
            // await Arrange();

            // Act
            var response = await DataHolder_Par_API.SendRequest();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
                    parResponse.Should().NotBeNull();
                    parResponse?.RequestURI.Should().NotBeNullOrEmpty();
                    // response?.RequestURI.Should().Be("TODO"); // TODO - need confirm the actual RequestURI is correct?
                    parResponse?.ExpiresIn.Should().Be(90);
                }
            }
        }

        [Theory]
        // Call PAR endpoint, with cdrArrangementId that the data holder is not associated with, or is unknown, should be rejected
        // [InlineData("TODO", HttpStatusCode.OK)] // ArrangementID associated with data holder
        // [InlineData("TODO", HttpStatusCode.BadRequest)] // Unassociated ArrangementID (ArrangementID not associated with data holder)
        [InlineData("foo", HttpStatusCode.BadRequest)] // Unknown ArrangementID
        public async Task AC06_Post_WithUnknownOrUnAssociatedCdrArrangementId_ShouldRespondWith_400BadRequest(string cdrArrangementId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // await Arrange();

            // Act
            var response = await DataHolder_Par_API.SendRequest(cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var expectedResponse = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_id is invalid"",
                            ""meta"": {}
                        }]
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData("client_credentials", HttpStatusCode.Created)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC07_Post_WithInvalidGrantType_ShouldRespondWith_400BadRequest(string grantType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // await Arrange();

            // Act
            var response = await DataHolder_Par_API.SendRequest(grantType: grantType);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_request"",
                        ""description"": ""Invalid grant_type""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.Created)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC08_Post_WithInvalidClientId_ShouldRespondWith_400BadRequest(string clientId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // await Arrange();

            // Act
            var response = await DataHolder_Par_API.SendRequest(clientId: clientId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_request"",
                        ""description"": ""Invalid client_id""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(CLIENTASSERTIONTYPE, HttpStatusCode.Created)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC09_Post_WithInvalidClientAssertionType_ShouldRespondWith_400BadRequest(string clientAssertionType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // await Arrange();

            // Act
            var response = await DataHolder_Par_API.SendRequest(clientAssertionType: clientAssertionType);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_request"",
                        ""description"": ""Invalid client_assertion_type""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC10_Revocation_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest(string clientAssertion, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // await Arrange();

            // Act
            var response = await DataHolder_Par_API.SendRequest(clientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_request"",
                        ""description"": ""Invalid client_assertion""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, HttpStatusCode.Created)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.BadRequest)] // ie different holder of key
        public async Task AC11_Post_WithDifferentHolderOfKey_ShouldRespondWith_400BadRequest(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Act
            // var response = await DataHolder_Par_API.SendRequest(clientAssertion: clientAssertion);
            var response = await DataHolder_Par_API.SendRequest(
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_request"",
                        ""description"": ""Invalid client_assertion""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, to create CdrArrangement
        public async Task AC02_Post_AuthorisationEndpoint_WithRequestUri_ShouldRespondWith_200OK_CdrArrangementId()
        {
            // Arrange
            // await Arrange();
            var response = await DataHolder_Par_API.SendRequest();
            if (response.StatusCode != HttpStatusCode.Created) throw new Exception("Error with PAR request - StatusCode");
            var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
            if (string.IsNullOrEmpty(parResponse?.RequestURI)) throw new Exception("Error with PAR request - RequestURI");
            if (parResponse?.ExpiresIn != 90) throw new Exception("Error with PAR request - ExpiresIn");

            // Act - Authorise with PAR RequestURI
            (var authCode, var idToken) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = parResponse.RequestURI
            }.Authorise();

            // Assert - Check we got an authCode and idToken
            using (new AssertionScope())
            {
                authCode.Should().NotBeNullOrEmpty();
                idToken.Should().NotBeNullOrEmpty();
            }

            // Act - Use the authCode to get token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            // Assert - Check we get back cdrArrangementId
            using (new AssertionScope())
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
            }
        }

        [Theory]
        [InlineData(
            HttpStatusCode.Redirect,
            BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
            "error=invalid_request_uri&error_description=The request uri has expired"
        )]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, but after requestUri has expired (90 seconds), should redirect to DH callback URI
        public async Task AC03_Post_AuthorisationEndpoint_WithRequestUri_After90Seconds_ShouldRespondWith_302Found_CallbackURI(HttpStatusCode expectedStatusCode, string expectedRedirectPath, string? expectedRedirectQuery = null)
        {
            static HttpClient CreateHttpClient()
            {
                var httpClientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };
                httpClientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable));
                var httpClient = new HttpClient(httpClientHandler);
                return httpClient;
            }

            const int PAR_EXPIRY_SECONDS = 90;

            // Arrange
            // await Arrange();
            var response = await DataHolder_Par_API.SendRequest();
            if (response.StatusCode != HttpStatusCode.Created) throw new Exception("Error with PAR request - StatusCode");
            var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
            if (string.IsNullOrEmpty(parResponse?.RequestURI)) throw new Exception("Error with PAR request - RequestURI");
            if (parResponse?.ExpiresIn != PAR_EXPIRY_SECONDS) throw new Exception("Error with PAR request - ExpiresIn");

            // Wait until PAR expires
            await Task.Delay((PAR_EXPIRY_SECONDS + 10) * 1000);

            var AuthorisationURL = new AuthoriseURLBuilder { RequestUri = parResponse.RequestURI }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);
            var authResponse = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                authResponse.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = authResponse?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect query
                if (expectedRedirectQuery != null)
                {
                    var redirectQuery = HttpUtility.UrlDecode(authResponse?.Headers?.Location?.Query.TrimStart('?'));
                    redirectQuery.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectQuery));
                }
            }
        }

        [Fact]
        // Call PAR endpoint, with existing CdrArrangementId, to get requestUri
        public async Task<(string? cdrArrangementId, string? requestUri)> AC04_Post_WithCdrArrangementId_ShouldRespondWith_201Created_RequestUri()
        {
            // Create a CDR arrangement
            static async Task<string> CreateCDRArrangement()
            {
                // Authorise
                (var authCode, var _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = USERID_JANEWILSON,
                    OTP = AUTHORISE_OTP,
                    SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                    Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS,
                }.Authorise();

                // Get token
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
                if (tokenResponse == null || tokenResponse?.CdrArrangementId == null) throw new Exception("Error getting CDRArrangementId");

                // Return CdrArrangementId
                return tokenResponse.CdrArrangementId;
            }

            // Arrange 
            // await Arrange();
            var cdrArrangementId = await CreateCDRArrangement();

            // Act - PAR with existing CdrArrangementId
            var response = await DataHolder_Par_API.SendRequest(cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
                parResponse.Should().NotBeNull();
                parResponse?.RequestURI.Should().NotBeNullOrEmpty();
                parResponse?.ExpiresIn.Should().Be(90);

                return (cdrArrangementId, parResponse?.RequestURI);
            }
        }

        [Fact]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, to update existing CdrArrangement
        public async Task AC05_Post_AuthorisationEndpoint_WithRequestUri_ShouldRespondWith_200OK_CdrArrangementId()
        {
            // Arrange
            // await Arrange();
            // Create CDR arrangement, call PAR and get RequestURI
            (var cdrArrangementId, var requestUri) = await AC04_Post_WithCdrArrangementId_ShouldRespondWith_201Created_RequestUri();
            if (string.IsNullOrEmpty(requestUri)) throw new Exception("requestUri is null");

            // Act - Authorise using requestURI
            (var authCode, var _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = requestUri
            }.Authorise();

            // Act - Use the authCode to get token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            // Assert - Check we get back cdrArrangementId
            using (new AssertionScope())
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();

                // Check the arrangement was updated (Arrangementid should be different)??
                // tokenResponse?.CdrArrangementId.Should().NotBe(cdrArrangementId);
            }
        }
    }
}
