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
                actual.Keys.Length.Should().Be(1);
                actual.Keys[0].kty.Should().Be("RSA");
                actual.Keys[0].use.Should().Be("sig");
                actual.Keys[0].kid.Should().Be("73AEFCAF807652A46E3316DB47E905E7B72652B2"); // TODO - MJS - This should be derived 
                actual.Keys[0].e.Should().Be("AQAB");
                actual.Keys[0].n.Should().Be("3k18UiQLnAL0yH9JvI75swnrUZU7cRhoWbijUqE8NnMy5yzwShJzW00YzAgyrevCgTR7Pi-TNMot0x6DLtTBOMtGqlsmteGJrS27Pw74voxVnLDLq0cvmS0GTiQaseilcE5S5OfLRUv7eQJC4Q4nBuAAy1kiyyFbUkiZPWxSNiH5xZcoVsIYlkShfIwOOKAy290mwNONtGKaik_SYsUHE_OeQQEpkgVk3Ajx0A6xIbRery2_B6EH5ZKa62_1jJNGfT0f82CyGL5U0z7YSZeve2pf3CESzL6_YQfulMgkrp21WQ3pikKiuDf3JS14NeuldjsjWecfGZDiBpq_J8iXtw"); // TODO - MJS - This should be derived
            }
        }
    }
}
