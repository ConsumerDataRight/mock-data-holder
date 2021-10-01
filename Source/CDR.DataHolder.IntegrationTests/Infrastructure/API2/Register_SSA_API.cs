using System.Net.Http;
using System.Threading.Tasks;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public static class Register_SSA_API
    {
        /// <summary>
        /// Get SSA from the Register
        /// </summary>
        public static async Task<string> GetSSA(string brandId, string softwareProductId, string ssaVersion,
            string jwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
            string jwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            // Get access token 
            var registerAccessToken = await new Infrastructure.AccessToken
            {
                URL = BaseTest.REGISTER_MTLS_TOKEN_URL,
                CertificateFilename = BaseTest.CERTIFICATE_FILENAME,
                CertificatePassword = BaseTest.CERTIFICATE_PASSWORD,
                JWT_CertificateFilename = jwtCertificateFilename,
                JWT_CertificatePassword = jwtCertificatePassword,
                ClientId = softwareProductId,
                Scope = "cdr-register:bank:read",
                ClientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
                GrantType = "client_credentials",
                Issuer = softwareProductId,
                Audience = BaseTest.REGISTER_MTLS_TOKEN_URL
            }.GetAsync();

            // Get the SSA 
            var response = await new Infrastructure.API
            {
                URL = $"https://localhost:7001/cdr-register/v1/banking/data-recipients/brands/{brandId}/software-products/{softwareProductId}/ssa",
                CertificateFilename = BaseTest.CERTIFICATE_FILENAME,
                CertificatePassword = BaseTest.CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                XV = ssaVersion,
                AccessToken = registerAccessToken
            }.SendAsync();

            var ssa = await response.Content.ReadAsStringAsync();

            return ssa;
        }
    }
}