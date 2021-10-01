using System;
using System.Security.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CDR.DataHolder.IdentityServer
{
    class Program
    {
        static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .CreateLogger();
            try
            {
                Log.Information("Starting web host", args);
                CreateHostBuilder(args).Build().Run();
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                .UseSerilog((ctx, cfg) =>
                {
                    cfg.ReadFrom.Configuration(ctx.Configuration);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel((context, serverOptions) =>
                    {
                        serverOptions.Configure(context.Configuration.GetSection("Kestrel"))
                                        .Endpoint("HTTPS", listenOptions =>
                                        {
                                            listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;
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
