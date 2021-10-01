using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Fixtures
{
    /// <summary>
    /// Purges DataHolders IdentityServer database and registers software product
    /// (in addition to operations performed by TestFixture)
    /// </summary>
    public class RegisterSoftwareProductFixture : TestFixture, IAsyncLifetime
    {
        new public async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            // Purge IdentityServer
            TestSetup.DataHolder_PurgeIdentityServer();

            // Register software product
            await TestSetup.DataHolder_RegisterSoftwareProduct();
        }

        new public async Task DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }

    // public class RegisterSoftwareProductFixture : IAsyncLifetime
    // {
    //     JWKS_Endpoint? jwks_endpoint;

    //     public DataHolder_AccessToken_Cache DataHolder_AccessToken_Cache { get; } = new();

    //     public async Task InitializeAsync()
    //     {
    //         // Patch Register
    //         TestSetup.Register_PatchRedirectUri();
    //         TestSetup.Register_PatchJwksUri();

    //         // Purge IdentityServer
    //         TestSetup.DataHolder_PurgeIdentityServer();

    //         // Stand-up JWKS endpoint
    //         jwks_endpoint = new JWKS_Endpoint(
    //             BaseTest.SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS,
    //             BaseTest.JWT_CERTIFICATE_FILENAME,
    //             BaseTest.JWT_CERTIFICATE_PASSWORD);
    //         jwks_endpoint.Start();

    //         // Register software product
    //         await TestSetup.DataHolder_RegisterSoftwareProduct();
    //     }

    //     public async Task DisposeAsync()
    //     {
    //         if (jwks_endpoint != null)
    //             await jwks_endpoint.DisposeAsync();

    //         // return Task.CompletedTask;
    //     }
    // }  
}
