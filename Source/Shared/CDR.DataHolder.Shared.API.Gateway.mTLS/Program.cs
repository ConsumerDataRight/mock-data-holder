using System;
using System.IO;
using System.Security.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CDR.DataHolder.Shared.API.Gateway.Mtls
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            // Get the value of the "industry" key from appsettings.json
            var industry = config["Industry"] ?? string.Empty;

            var configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json")
                            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.{industry}.json", optional: true, reloadOnChange: true)
                            .AddJsonFile($"gateway-config.{industry.ToLower()}.json", false, true)
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
                CreateHostBuilder(args, configuration).Build().Run();
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

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
            Host
                .CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(configuration);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel((context, serverOptions) =>
                        {
                            serverOptions.Configure(context.Configuration.GetSection("Kestrel"))
                                .Endpoint("HTTPS", listenOptions =>
                                {
                                    listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;
                                });
                        })
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseIISIntegration()
                        .ConfigureKestrel(o =>
                        {
                            o.ConfigureHttpsDefaults(o => o.ClientCertificateMode = ClientCertificateMode.RequireCertificate);
                        })
                        .UseStartup<Startup>();
                });
    }
}
