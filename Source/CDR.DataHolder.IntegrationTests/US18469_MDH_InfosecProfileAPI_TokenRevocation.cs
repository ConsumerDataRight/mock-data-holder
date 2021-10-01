using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US18469_MDH_InfosecProfileAPI_TokenRevocation : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        // Call authorise/token endpoints to create access and refresh tokens
        static private async Task<(string accessToken, string refreshToken)> CreateTokens()
        {
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null || tokenResponse.AccessToken == null || tokenResponse.RefreshToken == null)
                throw new Exception("Error getting access/refresh tokens");

            return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
        }

        [Fact]
        // Revoke an access token
        public async Task AC01_Post_WithAccessToken_ShouldRespondWith_200OK()
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(token: accessToken, tokenTypeHint: "access_token");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                await Assert_HasNoContent2(response.Content);
            }
        }

        /// <summary>
        /// Call resource api using the access token. 
        /// </summary>
        static private async Task<HttpStatusCode> CallResourceAPI(string accessToken)
        {
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            return response.StatusCode;
        }


        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // Try to use a revoked access token to call a resource API
        public async Task AC02_CallResourceAPI_WithRevokedAccessToken_ShouldRespondWith_401Unauthorised(bool revoke, HttpStatusCode expectedResourceAPIStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            using (new AssertionScope())
            {
                if (revoke)
                {
                    // Act - Revoke access token
                    var response = await DataHolder_TokenRevocation_API.SendRequest(token: accessToken, tokenTypeHint: "access_token");

                    // Assert
                    response.StatusCode.Should().Be(HttpStatusCode.OK);

                    await Assert_HasNoContent2(response.Content);
                }

                // Assert - Check call to resource API returns correct status code
                (await CallResourceAPI(accessToken)).Should().Be(expectedResourceAPIStatusCode);
            }
        }

        [Fact]
        // Revoke a refresh token
        public async Task AC03_Post_WithRefreshToken_ShouldRespondWith_200OK()
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (_, var refreshToken) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(token: refreshToken, tokenTypeHint: "refresh_token");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                await Assert_HasNoContent2(response.Content);
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // Try to use a revoked refresh token to get new access and refresh token
        public async Task AC04_CallTokenAPI_WithRevokedRefreshToken_ShouldRespondWith_401Unauthorised(bool revoke, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (_, var refreshToken) = await CreateTokens();

            using (new AssertionScope())
            {
                if (revoke)
                {
                    // Act - Revoke access token
                    var response = await DataHolder_TokenRevocation_API.SendRequest(token: refreshToken, tokenTypeHint: "refresh_token");

                    // Assert
                    response.StatusCode.Should().Be(HttpStatusCode.OK);

                    await Assert_HasNoContent2(response.Content);
                }

                // Assert - Requesting new tokens returns correct status code
                var responseMessage = await DataHolder_Token_API.SendRequest(grantType: "refresh_token", refreshToken: refreshToken);
                responseMessage.StatusCode.Should().Be(expectedStatusCode);
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke an invalid access token
        public async Task AC05_Post_WithInvalidAccessToken_ShouldRespondWith_200OK(string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(token: token, tokenTypeHint: "access_token");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assert_HasNoContent2(response.Content);
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke an invalid refresh token
        public async Task AC06_Post_WithInvalidRefreshToken_ShouldRespondWith_200OK(string token, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(token: token, tokenTypeHint: "refresh_token");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assert_HasNoContent2(response.Content);
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke access token with invalid token type hint
        public async Task AC07_Post_WithInvalidAccessTokenTypeHint_ShouldRespondWith_200OK(string tokenTypeHint, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(token: accessToken, tokenTypeHint: tokenTypeHint);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assert_HasNoContent2(response.Content);

                (await CallResourceAPI(accessToken)).Should().NotBe(HttpStatusCode.OK, "token should have been revoked");
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke refresh token with invalid token type hint
        public async Task AC08_Post_WithInvalidRefreshTokenTypeHint_ShouldRespondWith_200OK(string tokenTypeHint, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (_, var refreshToken) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(token: refreshToken, tokenTypeHint: tokenTypeHint);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assert_HasNoContent2(response.Content);

                // Assert - Requesting new tokens with revoked refresh token should fail
                (await DataHolder_Token_API.SendRequest(grantType: "refresh_token", refreshToken: refreshToken))
                    .StatusCode.Should().NotBe(HttpStatusCode.OK, "token should have been revoked");
            }
        }

        [Theory]
        // [InlineData(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, HttpStatusCode.OK)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.Unauthorized)] // ie different holder of key
        // Revoke an access token with different holder of key
        public async Task AC09_Post_AccessTokenWithDifferentHolderOfKey_ShouldRespondWith_401Unauthorised(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                jwt_certificateFilename: jwtCertificateFilename,
                jwt_certificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope())
            {
                // Assert status code
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    var expectedResponse = @"{""error"":""invalid_client""}";
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                
                    // Assert - Should be able to access resource API since token not revoked
                    (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
                }
            }
        }

        [Theory]
        // [InlineData(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, HttpStatusCode.OK)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.Unauthorized)] // ie different holder of key
        public async Task AC10_Post_RefreshTokenWithDifferentHolderOfKey_ShouldRespondWith_401Unauthorised(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (_, var refreshToken) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(
                token: refreshToken,
                tokenTypeHint: "refresh_token",
                jwt_certificateFilename: jwtCertificateFilename,
                jwt_certificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope())
            {
                // Assert status code
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    await Assert_HasNoContent2(response.Content);

                    // Assert - Requesting new tokens with refresh token should not fail, since refresh token should not have been revoked
                    (await DataHolder_Token_API.SendRequest(grantType: "refresh_token", refreshToken: refreshToken))
                        .StatusCode.Should().Be(HttpStatusCode.OK);
                }
            }
        }

        [Fact]
        // Revoke an access token with invalid grant type
        public async Task AC11_Post_AccessTokenWithInvalidGrantType_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                grantType: "foo");

            // Assert
            using (new AssertionScope())
            {
                // Assert status code
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // Assert error response
                var expectedResponse = @"{""error"":""unsupported_grant_type""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);

                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
            }
        }

        [Fact]
        // Revoke an access token with invalid client id
        public async Task AC12_Post_AccessTokenWithInvalidClientId_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                clientId: "foo");

            // Assert
            using (new AssertionScope())
            {
                // Assert status code
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // Assert error response
                var expectedResponse = @"{""error"":""invalid_client""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);

                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
            }
        }

        [Fact]
        // Revoke an access token with invalid client assertion type
        public async Task AC13_Post_AccessTokenWithInvalidClientAssertionType_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                clientAssertionType: "foo");

            // Assert
            using (new AssertionScope())
            {
                // Assert status code
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // Assert error response
                var expectedResponse = @"{""error"":""invalid_client""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);

                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
            }
        }

        [Fact]
        // Revoke an access token with invalid client assertion
        public async Task AC14_Post_AccessTokenWithInvalidClientAssertion_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                clientAssertion: "foo");

            // Assert
            using (new AssertionScope())
            {
                // Assert status code
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // Assert error response
                var expectedResponse = @"{""error"":""invalid_client""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);

                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
            }
        }

        [Fact]
        // Revoke an access token with client assertion signed with invalid certificate
        public async Task AC15_Post_AccessTokenWithClientAssertionSignedWithInvalidCert_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await DataHolder_TokenRevocation_API.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                jwt_certificateFilename: ADDITIONAL_CERTIFICATE_FILENAME, // ie this is not JWT_CERTIFICATE_FILENAME, hence it's not a valid certificate to sign JWT with
                jwt_certificatePassword: ADDITIONAL_CERTIFICATE_PASSWORD
                );

            // Assert
            using (new AssertionScope())
            {
                // Assert status code
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // Assert error response
                var expectedResponse = @"{""error"":""invalid_client""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);

                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
            }
        }
    }
}
