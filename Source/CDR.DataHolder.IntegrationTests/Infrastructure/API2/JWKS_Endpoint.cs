using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public partial class JWKS_Endpoint : IAsyncDisposable
    {
        /// <summary>
        /// Emulate a JWKS endpoint on url returning a JWKS for the given certificate
        /// </summary>
        public JWKS_Endpoint(string url, string certificateFilename, string certificatePassword)
        {
            Url = url;
            CertificateFilename = certificateFilename;
            CertificatePassword = certificatePassword;
        }

        public string Url { get; init; }
        private string Url_PathAndQuery => new Uri(Url).PathAndQuery;
        private int Url_Port => new Uri(Url).Port;
        public string CertificateFilename { get; init; }
        public string CertificatePassword { get; init; }

        private IWebHost? host;

        public void Start()
        {
            host = new WebHostBuilder()
                .UseKestrel(opts =>
                {
                    opts.ListenAnyIP(Url_Port, opts => opts.UseHttps());  // This will use the default developer certificate.  Use "dotnet dev-certs https" to install if necessary
                })
               .UseStartup<JWKSCallback_Startup>(_ => new JWKSCallback_Startup(this))
               .Build();

            host.RunAsync();
        }

        public async Task Stop()
        {
            if (host != null)
            {
                await host.StopAsync();
            }
        }

        bool disposed;
        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                await Stop();
                disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        class JWKSCallback_Startup
        {
            private JWKS_Endpoint Endpoint { get; init; }

            public JWKSCallback_Startup(JWKS_Endpoint endpoint)
            {
                this.Endpoint = endpoint;
            }

            public void Configure(IApplicationBuilder app)
            {
                app.UseHttpsRedirection();
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet(Endpoint.Url_PathAndQuery, async context =>
                    {
                        // Build JWKS and return
                        var jwks = JWKSBuilder.Build(Endpoint.CertificateFilename, Endpoint.CertificatePassword);
                        await context.Response.WriteAsJsonAsync<JWKSBuilder.JWKS>(jwks);
                    });
                });
            }

            public static void ConfigureServices(IServiceCollection services)
            {
                services.AddRouting();
            }
        }
    }
}