using System.Threading.Tasks;
using Microsoft.Playwright;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    static public class PlaywrightHelper2
    {
        public delegate Task ExecuteDelegate(IPage page);
        static public async Task Execute(ExecuteDelegate executeDelegate, bool createMedia = false, string? mediaPrefix = null)
        {
            await using var browserContext = await PlaywrightHelper.NewBrowserContextAsync(createMedia: createMedia);
            try
            {
                var page = await PlaywrightHelper.NewPageAsync(browserContext);
                try
                {
                    await executeDelegate(page);
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
            finally
            {
                if (createMedia)
                {
                    await browserContext.Tracing.StopAsync(new()
                    {
                        Path = $"{PlaywrightHelper.MEDIAFOLDER}/{mediaPrefix}_trace.zip",
                    });
                }

                await browserContext.CloseAsync();
            }
        }
    }
}