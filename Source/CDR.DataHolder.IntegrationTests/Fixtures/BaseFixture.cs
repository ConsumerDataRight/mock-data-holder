using System.Threading.Tasks;
using CDR.DataHolder.IntegrationTests.Infrastructure.API2;
using Xunit;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Fixtures
{
    /// <summary>
    /// Patches Register SoftwareProduct RedirectURI and JwksURI.
    /// Stands up JWKS endpoint.
    /// </summary>
    public class TestFixture : IAsyncLifetime
    {
        private JWKS_Endpoint? jwks_endpoint;

        public DataHolder_AccessToken_Cache DataHolder_AccessToken_Cache { get; } = new();

        public Task InitializeAsync()
        {
            // Patch Register
            TestSetup.Register_PatchRedirectUri();
            TestSetup.Register_PatchJwksUri();

            // Stand-up JWKS endpoint
            jwks_endpoint = new JWKS_Endpoint(
                BaseTest.SubstituteConstant(BaseTest.SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS),
                BaseTest.JWT_CERTIFICATE_FILENAME,
                BaseTest.JWT_CERTIFICATE_PASSWORD);
            jwks_endpoint.Start();

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (jwks_endpoint != null)
                await jwks_endpoint.DisposeAsync();
        }
    }   
}
