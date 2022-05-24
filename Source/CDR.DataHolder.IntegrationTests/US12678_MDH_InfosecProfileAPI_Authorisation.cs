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
    public class US12678_MDH_InfosecProfileAPI_Authorisation : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        static private void Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer(true);
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON)]
        public async Task AC01_Get_WithValidRequest_ShouldRespondWith_302Redirect_RedirectToRedirectURI_IdToken(TokenType tokenType)
        {
            // Arrange
            Arrange();

            // Act
            var tokenResponse = await GetToken(tokenType); // Perform E2E authorisaton/consentflow

            // Assert
            using (new AssertionScope())
            {
                tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS);
                tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
                tokenResponse.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse.CdrArrangementId.Should().NotBeNullOrEmpty();
            }
        }

        private static HttpClient CreateHttpClient()
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

        [Theory]
        [InlineData("code id_token", HttpStatusCode.Redirect, "<MDH_HOST>:8001/account/login")]
        [InlineData("foo",
            HttpStatusCode.Redirect,
            SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, // Unsuccessful request should redirect back to DR
            "error=invalid_request&error_description=Unsupported response_type value&state="
        )]
        public async Task AC02_Get_WithInvalidResponseType_ShouldRespondWith_302Redirect_ErrorResponse(string responseType, HttpStatusCode expectedStatusCode, string expectedRedirectPath, string? expectedRedirectFragment = null)
        {
            expectedRedirectPath = BaseTest.SubstituteConstant(expectedRedirectPath);

            // Arrange
            Arrange();
            var AuthorisationURL = new AuthoriseURLBuilder { ResponseType = responseType }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect fragment
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response?.Headers?.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [Theory]
        [InlineData(null, HttpStatusCode.Redirect, "<MDH_HOST>:8001/account/login")]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC03_Get_WithInvalidRequestBody_ShouldRespondWith_400BadRequest_ErrorResponse(string requestBody, HttpStatusCode expectedStatusCode, string? expectedRedirectPath = null)
        {
            if (expectedRedirectPath != null)
                expectedRedirectPath = BaseTest.SubstituteConstant(expectedRedirectPath);

            // Arrange
            Arrange();
            var AuthorisationURL = new AuthoriseURLBuilder { Request = requestBody }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check redirect path
                if (expectedRedirectPath != null)
                {
                    // Check redirect path
                    var redirectPath = response?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                    redirectPath.Should().Be(expectedRedirectPath);
                }

                // Assert - Check error response
                if (response?.StatusCode == HttpStatusCode.BadRequest)
                {
                }
            }
        }

        [Theory]
        [InlineData(SCOPE, HttpStatusCode.Redirect, "<MDH_HOST>:8001/account/login")] // Successful request should redirect to the DH login URI 
        [InlineData(SCOPE + " admin:metadata:update", // Additional unsupported scope should be ignored
            HttpStatusCode.Redirect,
            "<MDH_HOST>:8001/account/login")]
        public async Task AC04_Get_WithInvalidScope_ShouldRespondWith_200OK_Response(string scope, HttpStatusCode expectedStatusCode, string expectedRedirectPath, string? expectedRedirectFragment = null)
        {
            expectedRedirectPath = BaseTest.SubstituteConstant(expectedRedirectPath);

            // Arrange
            Arrange();
            var AuthorisationURL = new AuthoriseURLBuilder { Scope = scope }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);

            // Act
            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect fragment
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response?.Headers?.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [Theory]
        [InlineData(SCOPE, HttpStatusCode.Redirect, "<MDH_HOST>:8001/account/login")]
        [InlineData(SCOPE_WITHOUT_OPENID, HttpStatusCode.Redirect,
            SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, // Unsuccessful request should redirect back to DR
            "error=invalid_request&error_description=OpenID Connect requests MUST contain the openid scope value.&state=")]
        public async Task AC05_Get_WithScopeMissingOpenId_ShouldRespondWith_302Redirect_ErrorResponse(
            string scope,
            HttpStatusCode expectedStatusCode,
            string expectedRedirectPath,
            string? expectedRedirectFragment = null)
        {
            expectedRedirectPath = BaseTest.SubstituteConstant(expectedRedirectPath);

            // Arrange
            Arrange();
            var AuthorisationURL = new AuthoriseURLBuilder { Scope = scope }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect fragment
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response?.Headers?.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.Redirect, "<MDH_HOST>:8001/account/login")]
        [InlineData(SOFTWAREPRODUCT_ID_INVALID, HttpStatusCode.Redirect,
            SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, // Unsuccessful request should redirect back to DR
            "error=invalid_request&error_description=Invalid client ID.&state=")]
        public async Task AC06_Get_WithInvalidClientID_ShouldRespondWith_302Redirect_ErrorResponse(string clientId, HttpStatusCode expectedStatusCode, string expectedRedirectPath, string? expectedRedirectFragment = null)
        {
            expectedRedirectPath = BaseTest.SubstituteConstant(expectedRedirectPath);

            // Arrange
            Arrange();
            var AuthorisationURL = new AuthoriseURLBuilder { ClientId = clientId.ToLower() }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect fragment
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response?.Headers?.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, HttpStatusCode.Redirect)]
        [InlineData("https://localhost:9001/foo", HttpStatusCode.BadRequest)]
        public async Task AC07_Get_WithInvalidRedirectURI_ShouldRespondWith_400BadRequest_ErrorResponse(string redirectUri, HttpStatusCode expectedStatusCode)
        {
            redirectUri = BaseTest.SubstituteConstant(redirectUri);

            // Arrange
            Arrange();
            var AuthorisationURL = new AuthoriseURLBuilder { RedirectURI = redirectUri.ToLower() }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var expectedContent = @"{
                        ""error"": ""invalid_request""
                    }";
                    await Assert_HasContent_Json(expectedContent, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, HttpStatusCode.Redirect, "<MDH_HOST>:8001/account/login")]
        [InlineData(
            INVALID_CERTIFICATE_FILENAME,
            INVALID_CERTIFICATE_PASSWORD,
            HttpStatusCode.Redirect,
            SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, // Unsuccessful request should redirect back to DR
                                                                         // "error=invalid_client&error_description=Signature is not valid.&state="
            "error=invalid_request_object&error_description=Invalid JWT request&state="
        )]
        // AC says "JWT is not signed" and "JWT not valid", but presumably it means JWT not signed by valid key (ie this AC is about DifferentHolderOfKey)
        public async Task AC08_Get_WithUnsignedRequestBody_ShouldRespondWith_302Redirect_ErrorResponse(
            string jwt_certificateFilename,
            string jwt_certificatePassword,
            HttpStatusCode expectedStatusCode,
            string expectedRedirectPath,
            string? expectedRedirectFragment = null)
        {
            expectedRedirectPath = BaseTest.SubstituteConstant(expectedRedirectPath);

            // Arrange
            Arrange();
            var AuthorisationURL = new AuthoriseURLBuilder
            {
                JWT_CertificateFilename = jwt_certificateFilename,
                JWT_CertificatePassword = jwt_certificatePassword
            }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect query
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response?.Headers?.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [Fact]
        public async Task AC09_UI_WithInvalidCustomerId_UIShouldShow_IncorrectCustomerIdMessage()
        {
            // Arrange
            Func<Task> act = async () =>
            {
                (var authCode, var idToken) = await new DataHolder_Authorise_APIv2
                {
                    UserId = "foo",
                    OTP = BaseTest.AUTHORISE_OTP,
                    SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON
                }.Authorise();
            };

            // Act/Assert
            await act.Should().ThrowAsync<EDataHolder_Authorise_IncorrectCustomerId>();
        }

        [Fact]
        public async Task AC10_UI_WithInvalidOTP_UIShouldShow_IncorrectPasswordMessage()
        {
            // Arrange
            Func<Task> act = async () =>
            {
                (var authCode, var idToken) = await new DataHolder_Authorise_APIv2
                {
                    UserId = BaseTest.USERID_JANEWILSON,
                    OTP = "foo",
                    SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON
                }.Authorise();
            };

            // Act/Assert
            await act.Should().ThrowAsync<EDataHolder_Authorise_IncorrectOneTimePassword>();
        }
    }
}
