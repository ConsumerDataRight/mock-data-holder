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
}
