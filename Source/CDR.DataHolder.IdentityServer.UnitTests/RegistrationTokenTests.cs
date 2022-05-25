using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CDR.DataHolder.IdentityServer.UnitTests
{
    public class RegistrationTokenTests
    {
        public RegistrationTokenTests()
        {

        }

        [Fact]
        public async Task Create_Registration_Token_Success()
        {
            // Arrange
            var softwareProductId = "9381dad2-6b68-4879-b496-c1319d7dfbc9";
            var certificatesPath = Path.Combine(Directory.GetCurrentDirectory(), "Certificates");
            var ssaPath = Path.Combine(certificatesPath, "ssa.pfx");
            var ssaPublicPath = Path.Combine(certificatesPath, "ssa.pem");
            var ps256PublicPath = Path.Combine(certificatesPath, "ps256-public.pem");
            var ps256Path = Path.Combine(certificatesPath, "ps256-private.pfx");
            var es256Path = Path.Combine(certificatesPath, "es256-private.pfx");

            var inMemorySettings = new Dictionary<string, string> {
                {"SigningCertificatePublic:Path", ssaPublicPath},
                {"SigningCertificate:Path", ssaPath},
                {"SigningCertificate:Password", "#M0ckRegister#"},
                {"PS256SigningCertificatePublic:Path", ps256PublicPath},
                {"PS256SigningCertificate:Path", ps256Path},
                {"PS256SigningCertificate:Password", "#M0ckDataHolder#"},
                {"ES256SigningCertificate:Path", es256Path},
                {"ES256SigningCertificate:Password", "#M0ckDataHolder#"}
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var securityService = new SecurityService(configuration);

            //Create jwt token
            var strHeader = @"
            {
               ""alg"":""PS256"",
               ""typ"":""JWT"",
               ""kid"":""542A9B91600488088CD4D816916A9F4488DD2651""
            }
            ";

            var strPayload = @"
            {
               ""iss"":""" + softwareProductId + @""",
               ""iat"":1571808167,
               ""exp"":2147483646,
               ""jti"":""37747cd1c10545699f754adf28b73e31"",
               ""aud"":""https://secure.api.dataholder.com/issuer"",
               ""client_id"": ""42b03ea3-c8c3-48a7-a0b0-32186256f204"",
               ""redirect_uris"":[
                  ""https://fintechx.io/products/trackxpense/cb""
               ],
               ""token_endpoint_auth_signing_alg"":""PS256"",
               ""token_endpoint_auth_method"":""private_key_jwt"",
               ""grant_types"":[
                  ""client_credentials"",
                  ""authorization_code"",
                  ""refresh_token""
               ],
               ""response_types"":[""code id_token""],
               ""application_type"":""web"",
               ""id_token_signed_response_alg"":""PS256"",
               ""id_token_encrypted_response_alg"":""RSA-OAEP"",
               ""id_token_encrypted_response_enc"":""A256GCM"",
               ""request_object_signing_alg"":""PS256"",
               ""software_statement"":""eyJhbGciOiJQUzI1NiIsImtpZCI6IjU0MkE5QjkxNjAwNDg4MDg4Q0Q0RDgxNjkxNkE5RjQ0ODhERDI2NTEiLCJ0eXAiOiJKV1QifQ.ew0KICAicmVjaXBpZW50X2Jhc2VfdXJpIjogImh0dHBzOi8vZmludGVjaHguaW8iLA0KICAiaXNzIjogImNkci1yZWdpc3RlciIsDQogICJpYXQiOiAxNjE5MjMzMjIxLA0KICAiZXhwIjogMTYxOTIzMzgyMSwNCiAgImp0aSI6ICJjZmExY2QwMmQ3OTE0YmU2YjRkOWM0ZDc3YzA4MGFkNyIsDQogICJvcmdfaWQiOiAiMjBjMDg2NGItY2VlZi00ZGUwLTg5NDQtZWIwOTYyZjgyNWViIiwNCiAgIm9yZ19uYW1lIjogIkZpbmFuY2UgWCIsDQogICJjbGllbnRfbmFtZSI6ICJUcmFjayBYcGVuc2UiLA0KICAiY2xpZW50X2Rlc2NyaXB0aW9uIjogIkFwcGxpY2F0aW9uIHRvIGFsbG93IHlvdSB0byB0cmFjayB5b3VyIGV4cGVuc2VzIiwNCiAgImNsaWVudF91cmkiOiAiaHR0cHM6Ly9maW50ZWNoeC5pby9wcm9kdWN0cy90cmFja3hwZW5zZSIsDQogICJyZWRpcmVjdF91cmlzIjogWw0KICAgICJodHRwczovL2ZpbnRlY2h4LmlvL3Byb2R1Y3RzL3RyYWNreHBlbnNlL2NiIg0KICBdLA0KICAibG9nb191cmkiOiAiaHR0cHM6Ly9maW50ZWNoeC5pby9wcm9kdWN0cy90cmFja3hwZW5zZS9sb2dvLnBuZyIsDQogICJqd2tzX3VyaSI6ICJodHRwczovL2xvY2FsaG9zdDo3MDA1L2Nkci1yZWdpc3Rlci92MS9qd2tzIiwNCiAgInJldm9jYXRpb25fdXJpIjogImh0dHBzOi8vZmludGVjaHguaW8vcHJvZHVjdHMvdHJhY2t4cGVuc2UvcmV2b2tlIiwNCiAgInNvZnR3YXJlX2lkIjogIjkzODFkYWQyLTZiNjgtNDg3OS1iNDk2LWMxMzE5ZDdkZmJjOSIsDQogICJzb2Z0d2FyZV9yb2xlcyI6ICJkYXRhLXJlY2lwaWVudC1zb2Z0d2FyZS1wcm9kdWN0IiwNCiAgInNjb3BlIjogIm9wZW5pZCBiYW5rOmFjY291bnRzLmJhc2ljOnJlYWQgYmFuazphY2NvdW50cy5kZXRhaWw6cmVhZCBiYW5rOnRyYW5zYWN0aW9uczpyZWFkIGJhbms6cGF5ZWVzOnJlYWQgYmFuazpyZWd1bGFyX3BheW1lbnRzOnJlYWQgY29tbW9uOmN1c3RvbWVyLmJhc2ljOnJlYWQgY29tbW9uOmN1c3RvbWVyLmRldGFpbDpyZWFkIGNkcjpyZWdpc3RyYXRpb24iDQp9.Xa_cJNCKTq7Oq_AxfGw0rWIu6Y0qHmuNT7U2QLtdXPAMODQLKlQva4N4K3NT-ZbKu--rOHK17Vg4p5MmfbiuzB9LpI2_l_j7GTe3qfRPxvc2vgIysY-SkII2z6BbHNHEx7vx5ywXmYaoCUNo64GRaoEbNwGfZsliBsyP_aIixKKVB2U2xkYI2G1YLlLrIYjZA1bT7PEgPvw7EHdULOy8nE31pOmqtJO8emRoWA7xw_DYrntb1VSLOIVtO_BIG_IO41lePTlNZLlOHNyVBMpIddFR3SVWEr_3LzQv3OpUzZAJx53uBcuodTG8WNdtO-L-q1ONfNqgmKzNrzOPeZMhoQ""
            }
            ";
            JToken tokenHeader = JToken.Parse(strHeader);
            JToken tokenPayload = JToken.Parse(strPayload);

            var encHeader = Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(tokenHeader.ToString()));
            var encPayload = Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(tokenPayload.ToString()));

            var plaintext = $"{encHeader}.{encPayload}";
            var digest = Encoding.UTF8.GetBytes(plaintext);

            var signature = Base64UrlTextEncoder.Encode(await securityService.Sign("PS256", digest));

            var token = $"{plaintext}.{signature}";

            //Validate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();

            //Read token
            tokenHandler.ReadToken(token);

            //Create the certificate which has only public key
            var cert = new X509Certificate2(configuration["PS256SigningCertificatePublic:Path"], SecurityAlgorithms.RsaSsaPssSha256);

            //Get credentials from certificate
            var certificateSecurityKey = new X509SecurityKey(cert);

            //Set token validation parameters
            var validationParameters = new TokenValidationParameters()
            {
                IssuerSigningKey = certificateSecurityKey,
                ValidateIssuerSigningKey = true,
                ValidIssuer = softwareProductId,
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            //Act
            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            //Assert
            Assert.True(validatedToken != null);
            Assert.True(validatedToken.Issuer == softwareProductId);
        }

    }
}
