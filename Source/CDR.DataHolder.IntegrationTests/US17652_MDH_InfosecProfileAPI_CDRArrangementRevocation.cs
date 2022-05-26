using CDR.DataHolder.IntegrationTests.Extensions;
using CDR.DataHolder.IntegrationTests.Fixtures;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class US17652_MDH_InfosecProfileAPI_CDRArrangementRevocation : BaseTest, IClassFixture<TestFixture>
    {
        static private async Task Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer();
            await TestSetup.DataHolder_RegisterSoftwareProduct();
        }

        static int CountPersistedGrant(string persistedGrantType, string? key = null)
        {
            using var connection = new SqlConnection(IDENTITYSERVER_CONNECTIONSTRING);
            connection.Open();

            SqlCommand selectCommand;
            if (key != null)
            {
                selectCommand = new SqlCommand($"select count(*) from persistedgrants where type=@type and [key]=@key", connection);
                selectCommand.Parameters.AddWithValue("@key", key);
            }
            else
            {
                selectCommand = new SqlCommand($"select count(*) from persistedgrants where [type]=@type", connection);
            }
            selectCommand.Parameters.AddWithValue("@type", persistedGrantType);

            return selectCommand.ExecuteScalarInt32();
        }

        [Fact]
        // When an arrangement exists, revoking the arrangement should remove the arrangement and revoke any associated tokens
        public async Task AC01_Post_WithArrangementId_ShouldRespondWith_204NoContent_ArrangementRevoked()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Arrange - Get token and cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act - Revoke CDR arrangement
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);

                await Assert_HasNoContent2(response.Content);

                // Assert - Check persisted grants no longer exist
                CountPersistedGrant("refresh_token").Should().Be(0);
                CountPersistedGrant("cdr_arrangement_grant", cdrArrangementId).Should().Be(0);
            }
        }

        static async Task RevokeCdrArrangement(string cdrArrangementId)
        {
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: cdrArrangementId);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new Exception("Error revoking cdr arrangement");
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // When an arrangement has been revoked, trying to use associated access token should result in error (Unauthorised)
        public async Task AC02_GetAccounts_WithRevokedAccessToken_ShouldRespondWith_401Unauthorised(bool revokeArrangement, HttpStatusCode expectedStatusCode)
        {
            static async Task<HttpResponseMessage> GetAccounts(string? accessToken)
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

                return response;
            }

            // Arrange
            await Arrange();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Arrange - Get token response using authCode
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null || tokenResponse.AccessToken == null || tokenResponse.CdrArrangementId == null) throw new Exception("Unexpected token response");

            // Arrange - Revoke the arrangement
            if (revokeArrangement)
            {
                await RevokeCdrArrangement(tokenResponse.CdrArrangementId);
            }

            // Act - Use access token to get accounts. The access token should have been revoked because the arrangement was revoked
            var response = await GetAccounts(tokenResponse.AccessToken);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    await Assert_HasNoContent2(response.Content);
                }
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // When an arrangement has been revoked, trying to use associated refresh token to get newly minted access token should result in error (401Unauthorised)
        public async Task AC03_GetAccessToken_WithRevokedRefreshToken_ShouldRespondWith_401Unauthorised(bool revokeArrangement, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Arrange - Get token response using authCode
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null || tokenResponse.RefreshToken == null || tokenResponse.CdrArrangementId == null) throw new Exception("Unexpected token response");

            // Arrange - Revoke the arrangement
            if (revokeArrangement)
            {
                await RevokeCdrArrangement(tokenResponse.CdrArrangementId);
            }

            // Act - Use refresh token to get a new access token. The refresh token should have been revoked because the arrangement was revoked            
            var response = await DataHolder_Token_API.SendRequest(
                grantType: "refresh_token",
                refreshToken: tokenResponse?.RefreshToken,
                scope: US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check json
                    var expectedResponse = @"{""error"":""invalid_grant""}";
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid arrangementid should result in error (422UnprocessableEntity)
        public async Task AC04_POST_WithInvalidArrangementID_ShouldRespondWith_422UnprocessableEntity()
        {
            // Arrange
            await Arrange();

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: "foo");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

                var expectedResponse = @"{
                    ""errors"": [{
                        ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",
                        ""title"": ""Invalid Consent Arrangement"",
                        ""detail"": ""CDR arrangement ID is not valid"",
                        ""meta"": {}
                    }]
                }";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Fact]
        // Calling revocation endpoint with arrangementid that is not associated with the DataRecipient should result in error (422UnprocessableEntity)
        public async Task AC05_POST_WithNonAssociatedArrangementID_ShouldRespondWith_422UnprocessableEntity()
        {
            static async Task<JWKS_Endpoint> ArrangeAdditionalDataRecipient()
            {
                // Patch Register for additional data recipient
                TestSetup.Register_PatchRedirectUri(
                    ADDITIONAL_SOFTWAREPRODUCT_ID,
                    ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);
                TestSetup.Register_PatchJwksUri(
                    ADDITIONAL_SOFTWAREPRODUCT_ID,
                    ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS);

                // Stand-up JWKS endpoint for additional data recipient
                var jwks_endpoint = new JWKS_Endpoint(
                    ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS,
                    ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                    ADDITIONAL_JWKS_CERTIFICATE_PASSWORD);
                jwks_endpoint.Start();

                // Register software product for additional data recipient
                await TestSetup.DataHolder_RegisterSoftwareProduct(
                    ADDITIONAL_BRAND_ID,
                    ADDITIONAL_SOFTWAREPRODUCT_ID,
                    ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                    ADDITIONAL_JWKS_CERTIFICATE_PASSWORD);

                return jwks_endpoint;
            }

            // Arrange
            await Arrange();
            await using var additional_jwks_endpoint = await ArrangeAdditionalDataRecipient();

            // Arrange - Get authcode and thus create a CDR arrangement for ADDITIONAL_SOFTWAREPRODUCT_ID client
            (var additional_authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS,
                ClientId = ADDITIONAL_SOFTWAREPRODUCT_ID.ToLower(),
                RedirectURI = ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                JwtCertificateFilename = ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                JwtCertificatePassword = ADDITIONAL_JWKS_CERTIFICATE_PASSWORD
            }.Authorise();

            // Arrange - Get the cdrArrangementId created by ADDITIONAL_SOFTWAREPRODUCT_ID client
            var additional_cdrArrangementId = (await DataHolder_Token_API.GetResponse(
                additional_authCode,
                clientId: ADDITIONAL_SOFTWAREPRODUCT_ID,
                redirectUri: ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                jwkCertificateFilename: ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                jwkCertificatePassword: ADDITIONAL_JWKS_CERTIFICATE_PASSWORD
            ))?.CdrArrangementId;

            // Act - Have SOFTWAREPRODUCT_ID client attempt to revoke CDR arrangement created by ADDITIONAL_SOFTWAREPRODUCT_ID client
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: additional_cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

                // Assert - Check json
                var expectedResponse = @"{
                    ""errors"": [
                        {
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",
                            ""title"": ""Invalid Consent Arrangement"",
                            ""detail"": ""CDR Arrangement ID is not valid for the given client"",
                            ""meta"": {}
                        }
                    ]
                }";
                await Assert_HasContent_Json(expectedResponse, response.Content);

                // Assert - Check persisted grants still exist
                CountPersistedGrant("refresh_token").Should().Be(1);
                CountPersistedGrant("cdr_arrangement_grant", additional_cdrArrangementId).Should().Be(1);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientid should result in error (401Unauthorised)
        public async Task AC07_POST_WithInvalidClientId_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientId: "foo");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                await Assert_HasNoContent2(response.Content);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientassertiontype should result in error (401Unauthorised)
        public async Task AC08a_POST_WithInvalidClientAssertionType_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientAssertionType: "foo");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                await Assert_HasNoContent2(response.Content);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientassertion should result in error (401Unauthorised)
        public async Task AC08b_POST_WithInvalidClientAssertion_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientAssertion: "foo");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                await Assert_HasNoContent2(response.Content);
            }
        }

        [Theory]
        [InlineData(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, HttpStatusCode.NoContent)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.Unauthorized)]  // ie different holder of key
        // Calling revocation endpoint with different holder of key should result in error (401Unauthorised)
        public async Task AC09_POST_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorised(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_JANEWILSON,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_JANE_WILSON,
                Scope = US12963_MDH_InfosecProfileAPI_Token.SCOPE_TOKEN_ACCOUNTS
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.NoContent)
                {
                    await Assert_HasNoContent2(response.Content);
                }
            }
        }
    }
}
