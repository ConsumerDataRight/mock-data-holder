using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Security.Authentication;

namespace CDR.DataHolder.Admin.API
{
    public sealed class Program
    {
        private Program()
        {
        }

        public static int Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            // Get the value of the "industry" key from appsettings.json
            var industry = config["Industry"];

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.{industry}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty)
                .CreateLogger();

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
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel((context, serverOptions) =>
                    {
                        var industry = context.Configuration.GetValue<string>("Industry");
                        logger.Information("Industry is set to {Industry}", industry);
                        serverOptions.Configure(context.Configuration.GetSection("Kestrel"))
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
