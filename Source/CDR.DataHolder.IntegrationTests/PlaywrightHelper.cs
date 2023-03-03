#define TEST_DEBUG_MODE // Run Playwright in non-headless mode for debugging purposes (ie show a browser)

// In docker (Ubuntu container) Playwright will fail if running in non-headless mode, so we ensure TEST_DEBUG_MODE is undef'ed
#if !DEBUG
#undef TEST_DEBUG_MODE
#endif

using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

#nullable enable

namespace CDR.DataHolder.IntegrationTests
{
    static public class PlaywrightHelper
    {
        static public bool CREATE_MEDIA = false;
        static public string MEDIAFOLDER = "/tmp/media";

        private const int VIEWPORT_WIDTH = 1920;
        private const int VIEWPORT_HEIGHT = 1600;
        private const int RECORDVIDEO_WIDTH = VIEWPORT_WIDTH;
        private const int RECORDVIDEO_HEIGHT = VIEWPORT_HEIGHT;

        public static async Task ScreenshotAsync(IPage page, string prefix, string suffix = "")
        {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = $"{MEDIAFOLDER}/{prefix}{suffix}.png" });
        }

        public static async Task VideoAsync(IPage page, string prefix, string suffix = "")
        {
            if (page.Video != null)
            {
                var videoPath = await page.Video.PathAsync();
                File.Move(videoPath, $"{MEDIAFOLDER}/{prefix}{suffix}.webm");
            }
        }

        static private IPlaywright? playwright = null;
        static private IBrowser? browser = null;

        /// <summary>
        /// Return playwright instance (singleton)
        /// </summary>
        static public async Task<IPlaywright> PlaywrightAsync()
        {
            if (playwright == null)
            {
                playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            }

            return playwright;
        }

        /// <summary>
        /// Return browser instance (singleton)
        /// </summary>
        static public async Task<IBrowser> BrowserAsync()
        {
            if (browser == null)
            {
                // Chromium not working in container for some reason. Using firefox instead.
                browser = await (await PlaywrightAsync()).Firefox.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    SlowMo = 100,
                    // Headless = true,
#if TEST_DEBUG_MODE
                    Headless = false,
                    Timeout = 10000
#endif
                });
            }

            return browser;
        }

        /// <summary>
        /// Return a new browser context
        /// </summary>
        static public async Task<IBrowserContext> NewBrowserContextAsync(bool createMedia = false, string? storageState = null)
        {
            var options = new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                StorageState = storageState,
            };

            if (createMedia && CREATE_MEDIA == true)
            {
                options.ViewportSize = new ViewportSize
                {
                    Width = VIEWPORT_WIDTH,
                    Height = VIEWPORT_HEIGHT
                };
                options.RecordVideoDir = $"{MEDIAFOLDER}";
                options.RecordVideoSize = new RecordVideoSize
                {
                    Width = RECORDVIDEO_WIDTH,
                    Height = RECORDVIDEO_HEIGHT
                };
            }
            var context = await (await BrowserAsync()).NewContextAsync(options);

            if (createMedia)
            {
                await context.Tracing.StartAsync(new()
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                });
            }

            return context;
        }

        /// <summary>
        /// Return a new page in the browser context
        /// </summary>
        static public async Task<IPage> NewPageAsync(IBrowserContext context, string? mediaPrefix = null)
        {
            static void DeleteFile(string filename)
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }

            if (CREATE_MEDIA && mediaPrefix != null)
            {
                // Remove video/screens if they exist
                DeleteFile($"{MEDIAFOLDER}/{mediaPrefix}.webm");
                DeleteFile($"{MEDIAFOLDER}/{mediaPrefix}.png");
                DeleteFile($"{MEDIAFOLDER}/{mediaPrefix}-exception.png");
                DeleteFile($"{MEDIAFOLDER}/{mediaPrefix}_popup.webm");
                DeleteFile($"{MEDIAFOLDER}/{mediaPrefix}_popup.png");
                DeleteFile($"{MEDIAFOLDER}/{mediaPrefix}_trace.zip");
            }

            var page = await context.NewPageAsync();

            await page.SetViewportSizeAsync(VIEWPORT_WIDTH, VIEWPORT_HEIGHT);

            if (page.Video != null)
            {
#pragma warning disable CS4014 // we don't want to wait for these, theY will complete when the browser context is closed
                page.Video.SaveAsAsync($"{MEDIAFOLDER}/{mediaPrefix}.webm");
                page.Video.DeleteAsync();
#pragma warning restore CS4014                
            }

            return page;
        }
    }
}