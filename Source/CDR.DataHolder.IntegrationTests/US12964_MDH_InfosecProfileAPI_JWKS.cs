using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;

namespace CDR.DataHolder.IntegrationTests
{
    public class US12964_MDH_InfosecProfileAPI_JWKS : BaseTest
    {
#pragma warning disable IDE1006
        class AC1_Expected
        {
            public class Key
            {
                public string kty { get; set; }
                public string use { get; set; }
                public string kid { get; set; }
                public string e { get; set; }
                public string n { get; set; }
            }

            public Key[] Keys { get; set; }
        }
#pragma warning restore IDE1006

        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_ValidJWKS()
        {
            // Arrange

            // Act
            var response = await new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_TLS_IDENTITYSERVER_BASE_URL}/.well-known/openid-configuration/jwks",
            }.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check JWKS
                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<AC1_Expected>(actualJson);
                actual.Keys.Length.Should().Be(2);
                actual.Keys[0].kty.Should().Be("RSA");
                actual.Keys[0].use.Should().Be("sig");
                actual.Keys[0].kid.Should().Be("7C5716553E9B132EF325C49CA2079737196C03DB"); // MJS - This should be derived 
                actual.Keys[0].e.Should().Be("AQAB");
                actual.Keys[0].n.Should().Be("muidQL6h9QizbiZxZi3rpwNVDy7mXjtcl-C2rpI4JZzo0n2x-3KAHoCuuR7ZcX3b2DgfkI2IB9NsspdtZsAgKO0MYDROCn8TrIPKlvP4M8YwNQ1modLS9IfVqZU6Tp_mWpn89po7oZiTGq-qihv-xBUQwHM9FHplPP6DvA5Yl5UUHDdN2s9qnodjBI3SAyuVOY6s9X9iv-wDBYvI_981nEYA7Ndgm-QxW6qH0FgA8OC4yLE8e2QDEjL31JAXAJDcUTRTwiQL5jv_hd9Wze6_Oe19mcl1RKn1-z_96riylD3VrwqAR5KkmkyI35WBytAdUU1jpyT1D-RVxX-G3FHoUCgXPDSyvul9Djet65KZE1mkzZfCmo_2s44XcF_Mv4cBfayMdNkodu2EgTsBzgd7lmGszlDhEMZeLDELOIXdQRs5b6g7pt6YRRcGfDo6eRBuR6n9VCES5L9RNizUI--LISnM-W9tWxReGDoj6-YwLFq7bHNt42psvxJO96f3ISwn"); // MJS - This should be derived
            }
        }
    }
}
