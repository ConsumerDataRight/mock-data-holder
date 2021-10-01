using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataRecipientConsentCallback
    {
        public DataRecipientConsentCallback(string redirectUrl = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS) 
        {
            this.RedirectUrl = redirectUrl;

            this.Request = new CallbackRequest
            {
                PathAndQuery = new Uri(redirectUrl).PathAndQuery
            };
        }

        public string RedirectUrl { get; init; }
        private string RedirectUrl_LeftPart => new Uri(RedirectUrl).GetLeftPart(UriPartial.Authority);

        private IWebHost? host;

        public class CallbackRequest
        {
            public string? PathAndQuery { get; init; }
            public bool received = false;
            public string? body;
        }
        private CallbackRequest Request { get; init; }

        /// <summary>
        /// Start web host
        /// </summary>
        public void Start()
        {
            host = new WebHostBuilder()
               .ConfigureServices(s => { s.AddSingleton(typeof(CallbackRequest), Request); })
               .UseKestrel()
               .UseStartup<DataRecipientConsentCallback_Startup>()
               .UseUrls(RedirectUrl_LeftPart)
               .Build();

            host.RunAsync();
        }

        /// <summary>
        /// Stop web host
        /// </summary>
        public async Task Stop()
        {
            if (host != null)
            {
                await host.StopAsync();
            }
        }

        /// <summary>
        /// Wait until we get a callback or otherwise timeout
        /// </summary>
        public async Task<CallbackRequest?> WaitForCallback(int timeoutSeconds = 30)
        {
            var stopAt = DateTime.Now.AddSeconds(timeoutSeconds);

            // Keep checking until we timeout
            while (DateTime.Now < stopAt)
            {
                // Have we received the callback?
                if (Request.received)
                {
                    // Yes, so return the content
                    return Request;
                }

                // Otherwise wait another second
                await Task.Delay(1000);
            }

            return null; // Timed out
        }

        class DataRecipientConsentCallback_Startup
        {
            readonly CallbackRequest callbackRequest;

            public DataRecipientConsentCallback_Startup(CallbackRequest callbackRequest)
            {
                this.callbackRequest = callbackRequest;
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseHttpsRedirection();
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet(callbackRequest.PathAndQuery!, async context =>
                    {                        
                        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                        callbackRequest.body = body;
                        callbackRequest.received = true;
                    });

                    endpoints.MapPost(callbackRequest.PathAndQuery!, async context =>
                    {
                        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                        callbackRequest.body = body;
                        callbackRequest.received = true;
                    });
                });
            }

            public static void ConfigureServices(IServiceCollection services)
            {
                services.AddRouting();
            }
        }
    }

#if DEBUG
    public class TestDataRecipientConsentCallback
    {
        static HttpClient CreateHttpClient()
        {
            var httpClientHandler = new HttpClientHandler();

            httpClientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            httpClientHandler.ClientCertificates.Add(new X509Certificate2(BaseTest.CERTIFICATE_FILENAME, BaseTest.CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable));
            var httpClient = new HttpClient(httpClientHandler);

            return httpClient;
        }

        [Fact]
        public async Task Test()
        {
            var callback = new DataRecipientConsentCallback();
            callback.Start();
            try
            {
                var httpClient = CreateHttpClient();

                const string POSTEDCONTENT = "posted content";
                var response = await httpClient.PostAsync(callback.RedirectUrl, new StringContent(POSTEDCONTENT));
                var callbackResponse = await callback.WaitForCallback(10);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                callbackResponse?.received.Should().Be(true);
                callbackResponse?.body.Should().Be(POSTEDCONTENT);

                // var response = await httpClient.GetAsync(callback.RedirectUrl);
                // var callbackResponse = await callback.WaitForCallback(10);
                // response.StatusCode.Should().Be(HttpStatusCode.OK);
                // callbackResponse?.received.Should().Be(true);
            }
            finally
            {
                await callback.Stop();
            }
        }
    }
#endif    
}