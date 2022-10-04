using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;

namespace CDR.DataHolder.IntegrationTests
{
    public class US12962_MDH_InfosecProfileAPI_OIDCConfiguration : BaseTest
    {
#pragma warning disable IDE1006
        class AC1_Expected
        {
            public string issuer { get; set; }
            public string authorization_endpoint { get; set; }
            public string jwks_uri { get; set; }
            public string token_endpoint { get; set; }
            public string introspection_endpoint { get; set; }
            public string userinfo_endpoint { get; set; }
            public string registration_endpoint { get; set; }
            public string revocation_endpoint { get; set; }
            public string cdr_arrangement_revocation_endpoint { get; set; }
            public string pushed_authorization_request_endpoint { get; set; }
            public string[] claims_supported { get; set; }
            public string[] scopes_supported { get; set; }
            public string[] response_types_supported { get; set; }
            public string[] response_modes_supported { get; set; }
            public string[] grant_types_supported { get; set; }
            public string[] subject_types_supported { get; set; }
            public string[] id_token_signing_alg_values_supported { get; set; }
            public string[] token_endpoint_auth_signing_alg_values_supported { get; set; }
            public string[] token_endpoint_auth_methods_supported { get; set; }
            public string[] id_token_encryption_alg_values_supported { get; set; }
            public string[] id_token_encryption_enc_values_supported { get; set; }
            public string tls_client_certificate_bound_access_tokens { get; set; }
            public string[] acr_values_supported { get; set; }
        }
#pragma warning restore IDE1006

        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_OIDC()
        {
            // Act
            var response = await new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_TLS_IDENTITYSERVER_BASE_URL}/.well-known/openid-configuration",
            }.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<AC1_Expected>(actualJson);
                actual.issuer.Should().Be(DH_TLS_IDENTITYSERVER_BASE_URL);
                actual.authorization_endpoint.Should().Be($"{DH_TLS_IDENTITYSERVER_BASE_URL}/connect/authorize");
                actual.token_endpoint.Should().Be($"{DH_MTLS_GATEWAY_URL}/connect/token");
                actual.introspection_endpoint.Should().Be($"{DH_MTLS_GATEWAY_URL}/connect/introspect");
                actual.userinfo_endpoint.Should().Be($"{DH_MTLS_GATEWAY_URL}/connect/userinfo");
                actual.registration_endpoint.Should().Be($"{DH_MTLS_GATEWAY_URL}/connect/register");
                actual.jwks_uri.Should().Be($"{DH_TLS_IDENTITYSERVER_BASE_URL}/.well-known/openid-configuration/jwks");
                actual.pushed_authorization_request_endpoint.Should().Be($"{DH_MTLS_GATEWAY_URL}/connect/par");
                actual.revocation_endpoint.Should().Be($"{DH_MTLS_GATEWAY_URL}/connect/revocation");
                actual.cdr_arrangement_revocation_endpoint.Should().Be($"{DH_MTLS_GATEWAY_URL}/connect/arrangements/revoke");
                actual.scopes_supported.Should().BeEquivalentTo(new[] { "openid", "profile", "cdr:registration", "bank:accounts.basic:read", "bank:transactions:read", "common:customer.basic:read", });
                actual.claims_supported.Should().BeEquivalentTo(new[] { "name", "given_name", "family_name", "refresh_token_expires_at", "sharing_expires_at", "sharing_duration", "iss", "sub", "aud", "acr", "exp", "iat", "nonce", "auth_time", "updated_at" });
                actual.acr_values_supported.Should().IntersectWith(new[] { "urn:cds.au:cdr:2", "urn:cds.au:cdr:3" });
                actual.id_token_encryption_alg_values_supported.Should().IntersectWith(new[] { "RSA-OAEP", "RSA-OAEP-256" });
                actual.id_token_encryption_enc_values_supported.Should().IntersectWith(new[] { "A128CBC-HS256", "A256GCM" });
                actual.tls_client_certificate_bound_access_tokens.Should().Be("true");
                actual.response_types_supported.Should().BeEquivalentTo(new[] { "code id_token" });
                actual.id_token_signing_alg_values_supported.Should().BeEquivalentTo(new[] { "ES256", "PS256" });
                actual.token_endpoint_auth_signing_alg_values_supported.Should().BeEquivalentTo(new[] { "ES256", "PS256" });
                actual.token_endpoint_auth_methods_supported.Should().BeEquivalentTo(new[] { "private_key_jwt" });
                actual.subject_types_supported.Should().BeEquivalentTo(new[] { "pairwise" });
                actual.grant_types_supported.Should().BeEquivalentTo(new[] { "authorization_code", "client_credentials", "refresh_token" });
                actual.response_modes_supported.Should().BeEquivalentTo(new[] { "form_post", "fragment" });
            }
        }
    }
}
