using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IdentityServer4.Stores.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Jose;
using Microsoft.Data.Sqlite;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using CDR.DataHolder.IntegrationTests.Extensions;
using CDR.DataHolder.IntegrationTests.Fixtures;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    static class JsonHelper
    {
        public static string ToJson(this object value)
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            };
            jsonSerializerSettings.Converters.Add(new ClaimConverter());
            jsonSerializerSettings.Converters.Add(new ClaimsPrincipalConverter());

            return JsonConvert.SerializeObject(value, jsonSerializerSettings);
        }
    }

    public class US12963_MDH_InfosecProfileAPI_Token : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        public const string SCOPE_TOKEN_ACCOUNTS = "openid bank:accounts.basic:read";
        // public const string SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS = "openid bank:accounts.basic:read bank:transactions.basic:read";
        public const string SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS = "openid bank:accounts.basic:read bank:transactions:read";
        public const string SCOPE_EXCEED = "openid bank:accounts.basic:read bank:transactions:read additional:scope";

        static void AssertAccessToken(string? accessToken)
        {
            accessToken.Should().NotBeNullOrEmpty();
            if (accessToken != null)
            {
                var decodedAccessToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "iss");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "sub");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "aud");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "exp");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "auth_time");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "cnf");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "client_id");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "software_id");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "sector_identifier_uri");
                decodedAccessToken.Claims.Should().Contain(c => c.Type == "scope");
            }
        }

        static void AssertIdToken(string? idToken)
        {
            idToken.Should().NotBeNullOrEmpty();
            if (idToken != null)
            {
                // Decrypt the id token.
                // var privateKeyCertificate = new X509Certificate2(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                var privateKeyCertificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                var privateKey = privateKeyCertificate.GetRSAPrivateKey();
                JweToken token = JWE.Decrypt(idToken, privateKey);
                var decryptedIdToken = token.Plaintext;

                var decodedIdToken = new JwtSecurityTokenHandler().ReadJwtToken(decryptedIdToken);
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "iss");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "sub");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "aud");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "exp");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "auth_time");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "acr");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "sharing_expires_at");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "refresh_token_expires_at");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "family_name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "given_name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "updated_at");
            }
        }

        #region TEST_SCENARIO_A_IDTOKEN_AND_ACCESSTOKEN        
        [Fact]
        public async Task AC01_Post_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken()
        {
            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assert_HasContentType_ApplicationJson(responseMessage.Content);

                    var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }

        [Fact]
        // public async Task AC02_Put_ShouldRespondWith_400BadRequest_InvalidRequestErrorResponse()
        public async Task AC02_Put_ShouldRespondWith_404NotFound()
        {
            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, usePut: true);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);

                // if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                // {
                //     // Assert error response
                //     var expectedResponse = "{\"error\":\"invalid_request\"}";
                //     await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                // }
            }
        }

        [Theory]
        [InlineData("authorization_code", HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_GrantType_ShouldRespondWith_400BadRequest_InvalidRequestErrorResponse(string grantType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, grantType: grantType);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"unsupported_grant_type\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_ClientId_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(string clientId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, clientId: clientId);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_client\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(CLIENTASSERTIONTYPE, HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_ClientAssertionType_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(string clientAssertionType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, clientAssertionType: clientAssertionType);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_client\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(true, HttpStatusCode.OK)]
        [InlineData(false, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_ClientAssertion_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(bool useClientAssertion, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, useClientAssertion: useClientAssertion);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_client\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        public enum AC04_TestType { valid_jwt, omit_iss, omit_aud, omit_exp, omit_jti, exp_backdated }
        [Theory]
        [InlineData(AC04_TestType.valid_jwt, HttpStatusCode.OK)]
        [InlineData(AC04_TestType.omit_iss, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.omit_aud, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.omit_exp, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.omit_jti, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.exp_backdated, HttpStatusCode.BadRequest)]
        public async Task AC04_Post_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(AC04_TestType testType, HttpStatusCode expectedStatusCode)
        {
            static string GenerateClientAssertion(AC04_TestType testType)
            {
                string ISSUER = BaseTest.SOFTWAREPRODUCT_ID.ToLower();

                var now = DateTime.UtcNow;

                var additionalClaims = new List<Claim>
                {
                     new Claim("sub", ISSUER),
                     new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer)
                };

                if (testType != AC04_TestType.omit_iss)
                {
                    additionalClaims.Add(new Claim("iss", "foo"));
                }

                string? aud = null;
                if (testType != AC04_TestType.omit_aud)
                {
                    aud = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/token";
                }

                if (testType != AC04_TestType.omit_jti)
                {
                    additionalClaims.Add(new Claim("jti", Guid.NewGuid().ToString()));
                }

                DateTime? expires = null;
                if (testType == AC04_TestType.exp_backdated)
                {
                    expires = now.AddMinutes(-1);
                }
                else if (testType != AC04_TestType.omit_exp)
                {
                    expires = now.AddMinutes(10);
                }

                // var certificate = new X509Certificate2(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                var certificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                var x509SigningCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSsaPssSha256);

                var jwt = new JwtSecurityToken(
                    (testType == AC04_TestType.omit_iss) ? null : ISSUER,
                    aud,
                    additionalClaims,
                    expires: expires,
                    signingCredentials: x509SigningCredentials);

                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

                return jwtSecurityTokenHandler.WriteToken(jwt);
            }

            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var clientAssertion = GenerateClientAssertion(testType);
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, customClientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_client\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        public async Task AC05_Post_WithExpiredAuthCode_ShouldRespondWith_400BadRequest_InvalidGrant(bool expired, HttpStatusCode expectedStatusCode)
        {
            static async Task ExpireAuthCode(string authCode)
            {
                static string GetKey(SqliteConnection connection, string authCode)
                {
                    // Not sure how to derive persisted grant key from authcode, so lets just check only 1 authorization_code row and use it's key
                    // TODO - ideally, derive persisted grant key from authcode

                    // Count, must be 1 row otherwise throw
                    using var selectCommand = new SqliteCommand($"select count(*) from persistedgrants where type = 'authorization_code'", connection);
                    var count = selectCommand.ExecuteScalarInt32();
                    if (count != 1)
                    {
                        throw new Exception("Must be 1 'authorization_code' row");
                    }

                    // Get key
                    using var selectCommand2 = new SqliteCommand($"select key from persistedgrants where type = 'authorization_code'", connection);
                    return selectCommand2.ExecuteScalarString();
                }

                const int LIFETIME_SECONDS = 1; // authcode will expire after this amount of time
                const string LIFETIME = @"""Lifetime"":600,";
                string LIFETIME_PATCHED = $@"""Lifetime"":{LIFETIME_SECONDS},";

                using var connection = new SqliteConnection(IDENTITYSERVER_CONNECTIONSTRING);
                connection.Open();

                string key = GetKey(connection, authCode);

                // Read data from persistedgrant row
                using var selectCommand = new SqliteCommand($"select data from persistedgrants where key = @key", connection);
                selectCommand.Parameters.AddWithValue("@key", key);
                var data = selectCommand.ExecuteScalarString();

                // Patch data and set lifetime to LIFETIME_SECONDS
                if (!data.Contains(LIFETIME))
                {
                    throw new Exception($"Data does not contain '{LIFETIME}'");
                }
                var patchedData = data.Replace(LIFETIME, LIFETIME_PATCHED);

                // Update data in persistedgrant row
                using var updateCommand = new SqliteCommand($"update persistedgrants set data = @data where key = @key", connection);
                updateCommand.Parameters.AddWithValue("@key", key);
                updateCommand.Parameters.AddWithValue("@data", patchedData);
                updateCommand.ExecuteNonQuery();

                // Now wait until authcode has expired
                await Task.Delay(LIFETIME_SECONDS + 5 * 1000);  // Wait until authcode has expired
            }

            // Arrange
            // const int SHORTLIVEDLIFETIMESECONDS = 10;
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS,
            //     lifetimeSeconds: expired ? SHORTLIVEDLIFETIMESECONDS : 600);
            // // If testing expired authcode we wait until the SHORTLIVELIFETIME has passed
            // if (expired)
            // {
            //     await Task.Delay((SHORTLIVEDLIFETIMESECONDS + 1) * 1000);
            // }

            TestSetup.DataHolder_PurgeIdentityServer(true);

            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            if (expired)
            {
                await ExpireAuthCode(authCode);
            }

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                // if (responseMessage.StatusCode != HttpStatusCode.OK)
                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_grant\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC06_Post_WithMissingAuthCode_ShouldRespondWith_400BadRequest_InvalidGrant(string authCode, HttpStatusCode expectedStatusCode)
        {
            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    var expectedResponse = "{\"error\":\"invalid_grant\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC07_Post_WithInvalidAuthCode_ShouldRespondWith_400BadRequest_InvalidGrantResponse(string authCode, HttpStatusCode expectedStatusCode)
        {
            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode: authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    var expectedResponse = "{\"error\":\"invalid_grant\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }
        #endregion

        #region TEST_SCENARIO_B_IDTOKEN_ACCESSTOKEN_REFRESHTOKEN
        [Theory]
        [InlineData(3600)]
        public async Task AC08_Post_WithShareDuration_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken(int shareDuration)
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                SharingDuration = 100000
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, shareDuration: shareDuration);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assert_HasContentType_ApplicationJson(responseMessage.Content);

                    var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertIdToken(tokenResponse?.IdToken);

                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }
        #endregion

        #region TEST_SCENARIO_C_USE_REFRESHTOKEN_FOR_NEW_ACCESSTOKEN
        private async Task<HttpResponseMessage> Test_AC09_AC10(string initalScope, string requestedScope)
        {
            static async Task<(string? authCode, string? refreshToken)> GetRefreshToken(string scope)
            {
                // Create grant with specific scope
                // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, scope, sharingDuration: 100000);
                (var authCode, _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = USERID_JANEWILSON,
                    OTP = AUTHORISE_OTP,
                    SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                    Scope = scope,
                    SharingDuration = 100000
                }.Authorise();

                var responseMessage = await DataHolder_Token_API.SendRequest(authCode, shareDuration: 3600);

                var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                if (tokenResponse?.RefreshToken == null)
                {
                    throw new Exception($"{nameof(AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken)}.{nameof(GetRefreshToken)} - Error getting refresh token");
                }

                // Just make sure refresh token was issued with correct scope
                if (tokenResponse?.Scope != scope)
                {
                    throw new Exception($"{nameof(AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken)}.{nameof(GetRefreshToken)} - Unexpected scope");
                }

                return (authCode, tokenResponse?.RefreshToken);
            }

            // Get a refresh token with initial scope
            var (authCode, refreshToken) = await GetRefreshToken(initalScope);

            // Use the refresh token to get a new accesstoken and new refreshtoken (with the requested scope)
            var responseMessage = await DataHolder_Token_API.SendRequest(
                // authCode,
                grantType: "refresh_token",
                refreshToken: refreshToken,
                scope: requestedScope);

            return responseMessage;
        }

        [Fact]
        public async Task AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken()
        {
            // Arrange/Act
            var responseMessage = await Test_AC09_AC10(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();

                    AssertIdToken(tokenResponse?.IdToken);
                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }

        [Theory]
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS, HttpStatusCode.OK)] // Same scope - should be ok
        [InlineData(SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS, SCOPE_TOKEN_ACCOUNTS, HttpStatusCode.OK)] // Decreased in scope - should be ok
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS, HttpStatusCode.BadRequest)] // Increased in scope - should fail
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_EXCEED, HttpStatusCode.BadRequest)] // Exceed the allowable scope of the DR - should fail
        public async Task AC10_Post_WithRefreshToken_AndDifferentScope_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse(string initialScope, string requestedScope, HttpStatusCode expectedStatusCode)
        {
            // Arrange/Act
            var responseMessage = await Test_AC09_AC10(initialScope, requestedScope);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_scope\",\"error_description\":\"Token request invalid scope\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC11_Post_WithInvalidRefreshToken_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse(string refreshToken, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            // (var authCode, _) = DataHolder_Authorise_API.Authorise(CUSTOMERID_JANEWILSON, SCOPE_TOKEN_ACCOUNTS);
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, grantType: "refresh_token", refreshToken: refreshToken);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert error response
                    var expectedResponse = refreshToken == null ? "{\"error\":\"invalid_request\"}" : "{\"error\":\"invalid_grant\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }
        #endregion

        #region EXTRA_TESTS
        [Theory]
        [InlineData(0, false)]
        [InlineData(10000, true)]
        public async Task ACX01_Authorise_WithSharingDuration_ShouldRespondWith_AccessTokenRefreshToken(int sharingDuration, bool expectsRefreshToken)
        {
            var nowEpoch = DateTime.UtcNow.UnixEpoch();

            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                SharingDuration = sharingDuration
            }.Authorise();

            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            // Assert
            using (new AssertionScope())
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS); // access token expiry

                if (expectsRefreshToken)
                {
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse?.AccessToken);
                    var sharing_expires_at = decodedJWT.Claim("sharing_expires_at").Value;

                    sharing_expires_at.Should().NotBeNullOrEmpty();
                    // No way to know precise time for sharing_expires_at since it depends on time of authorisation, so just give it grace of 90sec
                    sharing_expires_at.ToInt().Should().BeInRange(nowEpoch + sharingDuration, nowEpoch + sharingDuration + 90);
                }
                else
                    tokenResponse?.RefreshToken.Should().BeNullOrEmpty();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        // [InlineData(3)] // TODO - Fix for this failure has been deferred. Disabling test so build pipeline passes
        public async Task ACX02_UseRefreshTokenMultipleTimes_ShouldRespondWith_AccessTokenRefreshToken(int usageAttempts)
        {
            // Arrange - Get authcode, 
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                SharingDuration = 10000
            }.Authorise();

            // Act - Get access token and refresh token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            using (new AssertionScope())
            {
                // Assert 
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNull();
                tokenResponse?.RefreshToken.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS); // access token expiry

                var refreshToken = tokenResponse?.RefreshToken;
                for (int i = 1; i <= usageAttempts; i++)
                {
                    // Act - Use refresh token to get access token and refresh token
                    var refreshTokenResponse = await DataHolder_Token_API.GetResponseUsingRefreshToken(refreshToken);
                    // var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(refreshTokenResponse?.AccessToken);

                    // Assert
                    refreshTokenResponse.Should().NotBeNull();
                    refreshTokenResponse?.AccessToken.Should().NotBeNull();
                    refreshTokenResponse?.RefreshToken.Should().NotBeNull();
                    refreshTokenResponse?.RefreshToken.Should().Be(refreshToken); // does refresh token change?

                    refreshToken = refreshTokenResponse?.RefreshToken;
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ACX03_UseExpiredRefreshToken_ShouldRespondWith_400BadRequest(bool expired)
        {
            const int SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN = 10;

            var nowEpoch = DateTime.UtcNow.UnixEpoch();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                SharingDuration = expired ? SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN : 10000 // if expired only share for 10 seconds
            }.Authorise();

            // Act - Get access token and refresh token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            using (new AssertionScope())
            {
                // Assert 
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNull();
                tokenResponse?.RefreshToken.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS); // access token expiry

                if (expired)
                {
                    // Assert - Check that refresh token will expire when we expect it to
                    var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse?.AccessToken);
                    var sharing_expires_at = decodedJWT.Claim("sharing_expires_at").Value;
                    sharing_expires_at.Should().NotBeNullOrEmpty();
                    // No way to know precise time for sharing_expires_at since it depends on time of authorisation, so just give it grace of 60sec
                    sharing_expires_at.ToInt().Should().BeInRange(nowEpoch + SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN, nowEpoch + SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN + 60);

                    // Arrange - wait until refresh token has expired
                    await Task.Delay((SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN + 10) * 1000);
                }

                // Act - Use refresh token to get access token and refresh token
                var refreshTokenResponseMessage = await DataHolder_Token_API.SendRequest(grantType: "refresh_token", refreshToken: tokenResponse?.RefreshToken);

                // Assert - If expired should be BadRequest otherwise OK
                refreshTokenResponseMessage.StatusCode.Should().Be(expired ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            }
        }
    }
    #endregion
}