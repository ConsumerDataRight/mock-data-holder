using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Collections;
using CDR.DataHolder.IntegrationTests.Infrastructure;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using CDR.DataHolder.IntegrationTests.Fixtures;

namespace CDR.DataHolder.IntegrationTests
{
    public class US15221_US12969_US15584_MDH_InfosecProfileAPI_Registration_POST : BaseTest, IClassFixture<TestFixture>
    {
        // Get payload from a JWT. Need to convert JArray claims into a string[] otherwise when we re-sign the token the JArray is not properly serialized.
        static private Dictionary<string, object> GetJWTPayload(JwtSecurityToken jwt)
        {
            var payload = new Dictionary<string, object>();

            foreach (var kvp in jwt.Payload)
            {
                // Need to process JArray as shown below because Microsoft.IdentityModel.Json.Linq.JArray is protected.
                if (kvp.Value.GetType().Name == "JArray")
                {
                    var list = new List<string>();

                    foreach (var item in kvp.Value as IEnumerable)
                    {
                        list.Add(item.ToString());
                    }

                    payload.Add(kvp.Key, list.ToArray());
                }
                else
                {
                    payload.Add(kvp.Key, kvp.Value);
                }
            }

            return payload;
        }

        [Theory]
        [InlineData("3")]      
        public async Task AC01_Post_WithUnregistedSoftwareProduct_ShouldRespondWith_201Created_CreatedProfile(string ssaVersion)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, ssaVersion);

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assert_HasContentType_ApplicationJson(response.Content);
                }
            }
        }

        [Fact]
        public async Task AC02_Post_WithRegistedSoftwareProduct_ShouldRespondWith_400BadRequest_DuplicateErrorResponse()
        {
            static async Task Arrange(string ssa)
            {
                TestSetup.DataHolder_PurgeIdentityServer();

                var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
                var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception("Unable to register software product");
                }
            }

            // Arrange
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");
            await Arrange(ssa);

            // Act - Try to register the same product again
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check application/json
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var expectedResponse = @"{
                    ""error"": ""invalid_client_metadata"",
                    ""error_description"": ""Duplicate registrations for a given software_id are not valid.""
                }";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Theory]
        [InlineData(true, HttpStatusCode.Created)]
        [InlineData(false, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithValidButUnapprovedSSA_400BadRequest_UnapprovedSSAErrorResponse(bool signedWithRegisterCertificate, HttpStatusCode expectedStatusCode)
        {
            // Fake a SSA by signing with certificate that is not the Register certificate
            static string CreateFakeSSA(string ssa)
            {
                var decodedSSA = new JwtSecurityTokenHandler().ReadJwtToken(ssa);

                var payload = GetJWTPayload(decodedSSA);

                // Sign with a non-SSA certicate (ie just use a data recipient certificate)
                var fakeSSA = JWT2.CreateJWT(
                    CERTIFICATE_FILENAME,
                    CERTIFICATE_PASSWORD,
                    payload);

                return fakeSSA;
            }

            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");

            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(
                signedWithRegisterCertificate ? ssa : CreateFakeSSA(ssa)
            );

            // Act
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Assert - Check json
                    var expectedResponse = @"{
                        ""error"": ""unapproved_software_statement"",
                        ""error_description"": ""Software statement is not approved by register""
                    }";
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC06_Post_WithInvalidSSAPayload_400BadRequest_InvalidSSAPayloadResponse()
        {
            // Arrange 
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa, redirect_uris: new string[] { "foo" });

            // Act
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check application/json
                Assert_HasContentType_ApplicationJson(response.Content);

                var expectedResponse = @"{
                    ""error"":""invalid_redirect_uri"",
                    ""error_description"":""One or more redirect uri is invalid""
                }";

                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Fact]
        public async Task AC07_Post_WithInvalidMetadata_400BadRequest_InvalidMetadataResponse()
        {
            // Arrange 
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa, token_endpoint_auth_signing_alg: "HS256"); // HS256 is invalid metadata (ie should be PS256)

            // Act
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check application/json
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var expectedResponse = @"{
                    ""error"":""invalid_client_metadata"",
                    ""error_description"":""The 'token_endpoint_auth_signing_alg' claim value must be one of 'PS256,ES256'.""
                }";

                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }
    }
}