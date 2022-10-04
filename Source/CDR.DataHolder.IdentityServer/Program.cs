using CDR.DataHolder.API.Infrastructure.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System;
using System.IO;
using System.Security.Authentication;

namespace CDR.DataHolder.IdentityServer
{
    public sealed class Program
    {
        private Program() { }

        public static int Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                .CreateLogger();

            Serilog.Debugging.SelfLog.Enable(msg => Log.Logger.Debug(msg));

            try
            {
                Log.Information("Starting web host", args);
                CreateHostBuilder(args, configuration, new SerilogLoggerFactory(Log.Logger).CreateLogger<Program>()).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                // StopTheHostException - Unhandled exception from a BackgroundService thrown out of
                // HostBuilder.Build() on adding a migration to a EF/.NET Core 6.0 RC2 project, needs to be dealt with gracefully.
                string type = ex.GetType().Name;
                if (type.Equals("StopTheHostException", StringComparison.Ordinal))
                {
                    throw;
                }

                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration, Microsoft.Extensions.Logging.ILogger logger) =>
                Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel((context, serverOptions) =>
                    {
                        serverOptions.Configure(context.Configuration.GetSection("Kestrel"))
                                        .Endpoint("HTTPS", listenOptions =>
                                        {
                                            listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;

                                            var tlsCertOverride = configuration.GetTlsCertificateOverride(logger);
                                            if (tlsCertOverride != null)
                                            {
                                                logger.LogInformation("TLS Certificate Override - {thumbprint}", tlsCertOverride.Thumbprint);
                                                listenOptions.HttpsOptions.ServerCertificate = tlsCertOverride;
                                            }
                                        });

                        serverOptions.ConfigureHttpsDefaults(options =>
                        {
                            options.SslProtocols = SslProtocols.Tls12;
                        });
                    })
                    .UseIIS();

                    webBuilder.UseStartup<Startup>();
                });
    }
}
