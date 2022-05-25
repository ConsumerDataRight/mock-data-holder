using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public static class DataHolder_Register_API
    {
        /// <summary>
        /// Create registration request JWT for SSA
        /// </summary>
        public static string CreateRegistrationRequest(
            string ssa,
            string token_endpoint_auth_signing_alg = "PS256",
            string[]? redirect_uris = null,
            string jwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string jwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            var decodedSSA = new JwtSecurityTokenHandler().ReadJwtToken(ssa);

            var softwareId = decodedSSA.Claims.First(claim => claim.Type == "software_id").Value;

            var iat = (Int32)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
            var exp = iat + 300; // expire 5 mins from now

            var subject = new Dictionary<string, object>
                {
                    { "iss", softwareId },
                    { "iat", iat },
                    { "exp", exp },
                    { "jti", Guid.NewGuid().ToString() },
                    // { "aud", $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/register" },
                    { "aud", BaseTest.REGISTRATION_AUDIENCE_URI },

                    // Get redirect_uris from SSA
                    { "redirect_uris",
                        redirect_uris ??
                        decodedSSA.Claims.Where(claim => claim.Type == "redirect_uris").Select(claim => claim.Value).ToArray() },

                    { "token_endpoint_auth_signing_alg", token_endpoint_auth_signing_alg },
                    { "token_endpoint_auth_method", "private_key_jwt" },
                    { "grant_types", new string[] { "client_credentials", "authorization_code", "refresh_token" } },
                    { "response_types", new string[] { "code id_token" }},
                    { "id_token_signed_response_alg", "PS256" },
                    { "id_token_encrypted_response_alg", "RSA-OAEP" },
                    { "id_token_encrypted_response_enc", "A256GCM" },
                    { "application_type", "web" }, // spec says optional
                    { "software_statement", ssa },

                    { "client_id",softwareId },
                };

            var jwt = JWT2.CreateJWT(
               jwtCertificateFilename,
               jwtCertificatePassword,
               subject);

            return jwt;
        }

        /// <summary>
        /// Register software product using registration request
        /// </summary>
        public static async Task<HttpResponseMessage> RegisterSoftwareProduct(string registrationRequest)
        {
            var url = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/register";

            var accessToken = new PrivateKeyJwt2()
            {
                CertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
                CertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD,
                Issuer = BaseTest.SOFTWAREPRODUCT_ID.ToLower(),
                Audience = url
            }.Generate();

            // Post the request
            var api = new Infrastructure.API
            {
                URL = url,
                CertificateFilename = BaseTest.CERTIFICATE_FILENAME,
                CertificatePassword = BaseTest.CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                AccessToken = accessToken,
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
            };
            var response = await api.SendAsync();

            return response;
        }
    }
}
