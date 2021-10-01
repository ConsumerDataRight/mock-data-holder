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
                // I know it's "clunky" but wasted too much time on this issue already...
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
        [InlineData("1")]
        [InlineData("2")]      
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

                    // await Assert_HasContent_Json(expected, response.Content);
                }
            }
        }

        // TODO - Don't think dynamic client registration works. AC01 above only works because the software product being registered already exists in MDH.DB softwareproduct table (from the seeddata)
        // TODO - Add extra test (ACX01) that attempts to register software product that doesn't exist in MDH.DB softwareproduct table
        // public async Task AC01X_Post_WithUnregistedSoftwareProduct_ShouldRespondWith_201Created_CreatedProfile(string ssaVersion)

        [Fact]
        public async Task AC02_Post_WithRegistedSoftwareProduct_ShouldRespondWith_400BadRequest_DuplicateErrorResponse()
        {
            static async Task Arrange(string ssa)
            {
                // TestSetup.Register_PatchRedirectUri();
                TestSetup.DataHolder_PurgeIdentityServer();

                var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
                var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception("Unable to register software product");
                }
            }

            // Arrange
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "2");
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
                // var expectedResponse = @"{
                //     ""errors"": [
                //         {
                //         ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidConsent"",
                //         ""title"": ""Consent Is Invalid"",
                //         ""detail"": ""Duplicate registrations for a given software_id are not valid"",
                //         ""meta"": {}
                //         }
                //     ]
                // }";
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

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "2");

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
                    // var expectedResponse = @"{
                    //     ""errors"": [{
                    //         ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidConsent"",
                    //         ""title"": ""Consent Is Invalid"",
                    //         ""detail"": ""Software statement is not approved by register"",
                    //         ""meta"": {}
                    //     }]
                    // }";
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        /* AC04 removed
        [Fact]
        public void AC04_Post_WithInvalidSSA_400BadRequest_InvalidSSAErrorResponse()
        {
            // Seems no different to AC05
        }
        */

        /* AC05 removed
        [Fact]
        public async Task AC05_Post_WithInvalidSSA_IAT_400BadRequest_InvalidSSAErrorResponse()
        {
            static string CreateInvalidSSA(string ssa)
            {
                var decodedSSA = new JwtSecurityTokenHandler().ReadJwtToken(ssa);

                var payload = GetJWTPayload(decodedSSA);

                // Make the SSA invalid by replacing iat claim with invalid value "foo"
                payload.Remove("iat");
                payload.Add("iat", "foo");

                // Re-sign the now invalid SSA
                var invalidSSA = JWT2.CreateJWT(SSA_CERTIFICATE_FILENAME, SSA_CERTIFICATE_PASSWORD, payload);

                return invalidSSA;
            }

            // Arrange 
            PurgeIdentityServerDatabase();

            var ssa = await GetSSA(BRANDID, SOFTWAREPRODUCTID, "2");
            string invalidSSA = CreateInvalidSSA(ssa);

            var registrationRequest = CreateRegistrationRequest(invalidSSA);

            // Act
            var response = await RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check application/json
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var expectedResponse = @"{
                    ""error"": ""invalid_software_statement"",
                    ""error_description"": ""Software statement is invalid""
                }";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }
        */

        [Fact]
        public async Task AC06_Post_WithInvalidSSAPayload_400BadRequest_InvalidSSAPayloadResponse()
        {
            // Arrange 
            // PurgeIdentityServerDatabase();
            // TestSetup.Register_PatchRedirectUri();
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "2");
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

                // var expectedResponse = @"{
                //     ""error"":""invalid_redirect_uri"",
                //     ""error_description"":""The 'redirect_uris' claim value must be one of 'https://api.mocksoftware/mybudgetapp/callback,https://api.mocksoftware/mybudgetapp/return'.""
                // }";

                var expectedResponse = @"{
                    ""error"":""invalid_redirect_uri"",
                    ""error_description"":""One or more redirect uri is invalid""
                }";

                // var expectedResponse = $@"{{
                //     ""errors"": [{{
                //         ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidConsent"",
                //         ""title"": ""Consent Is Invalid"",
                //         ""detail"": ""The 'redirect_uris' claim value must be one of '{SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS}'"",
                //         ""meta"": {{}}
                //     }}]
                // }}";

                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Fact]
        public async Task AC07_Post_WithInvalidMetadata_400BadRequest_InvalidMetadataResponse()
        {
            // Arrange 
            // PurgeIdentityServerDatabase();
            // TestSetup.Register_PatchRedirectUri();
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "2");
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

                // var expectedResponse = @"{
                //     ""errors"": [{
                //         ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidConsent"",
                //         ""title"": ""Consent Is Invalid"",
                //         ""detail"": ""The 'token_endpoint_auth_signing_alg' claim value must be one of 'PS256,ES256'."",
                //         ""meta"": {}
                //     }]
                // }";

                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }
    }
}