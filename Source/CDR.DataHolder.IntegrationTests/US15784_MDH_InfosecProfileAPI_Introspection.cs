using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US12966_MDH_InfosecProfileAPI_Introspection : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private RegisterSoftwareProductFixture Fixture { get; init; }

        public US12966_MDH_InfosecProfileAPI_Introspection(RegisterSoftwareProductFixture fixture)
        {
            Fixture = fixture;
        }

        static private void Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer(true);
        }

        class IntrospectionResponse
        {
#pragma warning disable IDE1006
            public bool? active { get; set; }
            public string? scope { get; set; }
            public int? exp { get; set; }
            public string? cdr_arrangement_id { get; set; }
#pragma warning restore IDE1006
        }

        // Sort space delimited array, eg Sort("dog cat frog") returns "cat dog frog"
        private static string? Sort(string? s)
        {
            if (s == null)
            {
                return null;
            }

            var array = s.Split(' ');

            Array.Sort(array);

            return String.Join(' ', array);
        }

        [Theory]
        [InlineData(TokenType.JANE_WILSON)]
        public async Task AC01_Post_ShouldRespondWith_200OK_IntrospectionInfo(TokenType tokenType)
        {
            const int REFRESHTOKEN_LIFETIME_SECONDS = 7776000;
            const int EXPIRY_GRACE_SECONDS = 120; // Grace period for expiry check (ie window size)

            // Arrange
            Arrange();
            var approximateGrantTime_Epoch = (Int32)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
            var tokenResponse = await GetToken(tokenType);

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(token: tokenResponse.RefreshToken);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(true);
                Sort(actual?.scope).Should().Be(Sort(tokenResponse.Scope));
                actual?.cdr_arrangement_id.Should().Be(tokenResponse.CdrArrangementId);

                // Check expiry of refresh token. 
                // Since we only know approximate time that refresh token was granted, we can only know approximate time refresh token will expire
                var approximateExpiryTime_Epoch = approximateGrantTime_Epoch + REFRESHTOKEN_LIFETIME_SECONDS;
                // So expiry time is approximated, check that actual expiry is within small window of the approximate expiry time
                actual?.exp.Should().BeInRange(
                    approximateExpiryTime_Epoch - EXPIRY_GRACE_SECONDS,
                    approximateExpiryTime_Epoch + EXPIRY_GRACE_SECONDS);
            }
        }

        [Theory]
        [InlineData(false, true)] // valid refresh token, expect active to be true
        [InlineData(true, false)] // invalid refresh token, expect active to be false
        public async Task AC02_Post_WithInvalidRefreshToken_ShouldRespondWith_200OK_ActiveFalse(bool invalidRefreshToken, bool expectedActive)
        {
            // Arrange
            Arrange();
            var tokenResponse = await GetToken(TokenType.JANE_WILSON);

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(
                token: invalidRefreshToken ? "foo" : tokenResponse.RefreshToken
            );

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(expectedActive);
            }
        }

        [Theory]
        [InlineData(false, true)] // valid refresh token, expect active to be true
        [InlineData(true, false)] // missing refresh token, expect active to be false
        public async Task AC03_Post_WithMissingRefreshToken_ShouldRespondWith_200OK_ActiveFalse(bool missingRefreshToken, bool expectedActive)
        {
            // Arrange
            Arrange();
            var tokenResponse = await GetToken(TokenType.JANE_WILSON);

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(
                token: missingRefreshToken ? null : tokenResponse.RefreshToken,
                tokenTypeHint: missingRefreshToken ? null : "refresh_token"
             );

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(expectedActive);
            }
        }

        [Theory]
        [InlineData(false, true)] // valid refresh token, expect active to be true
        [InlineData(true, false)] // expired refresh token, expect active to be false
        public async Task AC04_Post_WithExpiredRefreshToken_ShouldRespondWith_200OK_ActiveFalse(bool expired, bool expectedActive)
        {
            // Arrange
            Arrange();

            DataHolder_Token_API.Response? tokenResponse;
            if (expired)
            {
                const int EXPIRED_LIFETIME_SECONDS = 10;

                tokenResponse = await GetToken(TokenType.JANE_WILSON, tokenLifetime: EXPIRED_LIFETIME_SECONDS, sharingDuration: EXPIRED_LIFETIME_SECONDS);

                // Wait for token to expire
                await Task.Delay((EXPIRED_LIFETIME_SECONDS + 5) * 1000);
            }
            else
            {
                tokenResponse = await GetToken(TokenType.JANE_WILSON);
            }

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(token: tokenResponse.RefreshToken);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(expectedActive);
            }
        }

        [Theory]
        [InlineData("client_credentials", HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.Unauthorized)]
        public async Task AC05_Post_WithInvalidGrantType_ShouldRespondWith_401Unauthorized_ErrorResponse(string grantType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            Arrange();
            var tokenResponse = await GetToken(TokenType.JANE_WILSON);

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(
                token: tokenResponse.RefreshToken,
                grantType: grantType
            );

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var expectedContent = @"{
                        ""error"": ""unsupported_grant_type""
                    }";

                    await Assert_HasContent_Json(expectedContent, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.Unauthorized)]
        public async Task AC06_Post_WithInvalidClientId_ShouldRespondWith_401Unauthorized_ErrorResponse(string clientId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            Arrange();
            var tokenResponse = await GetToken(TokenType.JANE_WILSON);

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(
                token: tokenResponse.RefreshToken,
                clientId: clientId
            );

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var expectedContent = @"{
                        ""error"": ""invalid_client""
                    }";
                    await Assert_HasContent_Json(expectedContent, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(CLIENTASSERTIONTYPE, HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.Unauthorized)]
        public async Task AC07_Post_WithInvalidClientAssertionType_ShouldRespondWith_401Unauthorized_ErrorResponse(string clientAssertionType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            Arrange();
            var tokenResponse = await GetToken(TokenType.JANE_WILSON);

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(
                token: tokenResponse.RefreshToken,
                clientAssertionType: clientAssertionType
            );

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var expectedContent = @"{
                        ""error"": ""invalid_client""
                    }";
                    await Assert_HasContent_Json(expectedContent, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(null, HttpStatusCode.OK)] // valid client assertion
        [InlineData("foo", HttpStatusCode.Unauthorized)]
        public async Task AC08_Post_WithInvalidClientAssertion_ShouldRespondWith_401Unauthorized_ErrorResponse(string clientAssertion, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            Arrange();
            var tokenResponse = await GetToken(TokenType.JANE_WILSON);

            // Act
            var response = await DataHolder_Introspection_API.SendRequest(
                token: tokenResponse.RefreshToken,
                clientAssertion: clientAssertion
            );

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var expectedContent = @"{
                        ""error"": ""invalid_client""
                    }";
                    await Assert_HasContent_Json(expectedContent, response.Content);
                }
            }
        }
    }
}
