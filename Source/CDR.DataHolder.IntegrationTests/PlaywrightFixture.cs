using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;
using System;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    public class PlaywrightFixture : IAsyncLifetime
    {
        static private bool RUNNING_IN_CONTAINER => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.ToUpper() == "TRUE";        
        
        virtual public Task InitializeAsync()
        {
            Debug.WriteLine($"{nameof(PlaywrightFixture)}.{nameof(InitializeAsync)}");

            // Only install Playwright if not running in container, since Dockerfile.e2e-tests already installed Playwright
            if (!RUNNING_IN_CONTAINER)
            {
                // Ensure that Playwright has been fully installed.
                Microsoft.Playwright.Program.Main(new string[] { "install" });
                Microsoft.Playwright.Program.Main(new string[] { "install-deps" });
            }

            return Task.CompletedTask;
        }

        virtual public Task DisposeAsync()
        {
            Debug.WriteLine($"{nameof(PlaywrightFixture)}.{nameof(DisposeAsync)}");

            return Task.CompletedTask;
        }
    }
}
