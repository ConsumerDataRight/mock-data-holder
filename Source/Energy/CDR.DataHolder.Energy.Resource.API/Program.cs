using CDR.DataHolder.Energy.Resource.API;
using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
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

namespace CDR.DataHolder.Resource.API
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
                .MinimumLevel.Override("System", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty)
                .CreateLogger();

            Serilog.Debugging.SelfLog.Enable(msg => Log.Logger.Debug(msg));

            try
            {
                Log.Information("Starting web host");
                CreateHostBuilder(args, configuration, Log.Logger).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration, Serilog.ILogger logger) =>
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
                                                        logger.Information("TLS Certificate Override - {thumbprint}", tlsCertOverride.Thumbprint);
                                                        listenOptions.HttpsOptions.ServerCertificate = tlsCertOverride;
                                                    }
                                                });

                                serverOptions.ConfigureHttpsDefaults(options =>
                                {
                                    options.SslProtocols = SslProtocols.Tls12;
                                });
                            })
                            .UseIIS()
                            .UseStartup<Startup>();
                        });
    }
}
