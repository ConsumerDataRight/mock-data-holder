using CDR.DataHolder.Shared.Domain.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CDR.DataHolder.Energy.Tests.IntegrationTests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
             .AddEnvironmentVariables()
             .Build();

            // Setting up logger early so we can catch any startup issues
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration: configuration)
                .CreateBootstrapLogger();

            Log.Information($"---Logger has been configured within {nameof(Startup.ConfigureServices)}.---");

            services.AddMvc().AddCdrNewtonsoftJson();

            services.AddTestAutomationServices(configuration);
            services.AddTestAutomationSettings(opt =>
            {
                opt.INDUSTRY = Industry.ENERGY;
                opt.SCOPE = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants.Scopes.ScopeEnergy;

                opt.DH_MTLS_GATEWAY_URL = configuration["URL:DH_MTLS_Gateway"] ?? string.Empty;
                opt.DH_TLS_AUTHSERVER_BASE_URL = configuration["URL:DH_TLS_AuthServer"] ?? string.Empty;
                opt.DH_TLS_PUBLIC_BASE_URL = configuration["URL:DH_TLS_Public"] ?? string.Empty;
                opt.REGISTER_MTLS_URL = configuration["URL:Register_MTLS"] ?? string.Empty;

                // Connection strings
                opt.DATAHOLDER_CONNECTIONSTRING = configuration["ConnectionStrings:DataHolder"] ?? string.Empty;
                opt.AUTHSERVER_CONNECTIONSTRING = configuration["ConnectionStrings:AuthServer"] ?? string.Empty;
                opt.REGISTER_CONNECTIONSTRING = configuration["ConnectionStrings:Register"] ?? string.Empty;

                // Seed-data offset
                opt.SEEDDATA_OFFSETDATES = configuration["SeedData:OffsetDates"] == "true";

                opt.MDH_INTEGRATION_TESTS_HOST = configuration["URL:MDH_INTEGRATION_TESTS_HOST"] ?? string.Empty;
                opt.MDH_HOST = configuration["URL:MDH_HOST"] ?? string.Empty;

                opt.CDRAUTHSERVER_SECUREBASEURI = configuration["URL:CDRAuthServer_SecureBaseUri"] ?? string.Empty;
            });
        }
    }
}
