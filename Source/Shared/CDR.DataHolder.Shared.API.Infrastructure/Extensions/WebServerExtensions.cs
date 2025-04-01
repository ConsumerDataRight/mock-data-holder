using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Security.Authentication;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class WebServerExtensions
    {
        public static void ConfigureWebServer(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                var industry = configuration.GetValue<string>("Industry");
                logger.Information("Industry is set to {Industry}", industry);

                options.Configure(configuration.GetSection("Kestrel"))
                   .Endpoint("HTTPS", listenOptions =>
                   {
                       listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;

                       var tlsCertOverride = configuration.GetTlsCertificateOverride(logger);
                       if (tlsCertOverride != null)
                       {
                           logger.Information("TLS Certificate Override - {Thumbprint}", tlsCertOverride.Thumbprint);
                           listenOptions.HttpsOptions.ServerCertificate = tlsCertOverride;
                       }
                   });

                options.ConfigureHttpsDefaults(options =>
                {
                    options.SslProtocols = SslProtocols.Tls12;
                });
            });
        }
    }
}
