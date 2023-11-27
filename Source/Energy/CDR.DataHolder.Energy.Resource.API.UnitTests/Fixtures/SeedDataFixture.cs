using CDR.DataHolder.Energy.Domain.Repositories;
using CDR.DataHolder.Energy.Repository;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using CDR.DataHolder.Shared.Domain.Repositories;
using CDR.DataHolder.Shared.Repository;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CDR.DataHolder.Energy.Resource.API.UnitTests.Fixtures
{
    public class SeedDataFixture
    {
        public IServiceProvider ServiceProvider { get; set; }

        public SeedDataFixture()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            var services = new ServiceCollection();

            var connectionStr = configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default);
            Log.Logger.Information($"SQL Db ConnectionString: {connectionStr}");
            services.AddDbContext<EnergyDataHolderDatabaseContext>(options => options.UseSqlServer(connectionStr));

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            services.AddAutoMapper(typeof(Startup), typeof(EnergyDataHolderDatabaseContext));

            services.AddScoped<IEnergyResourceRepository, EnergyResourceRepository>();
            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddSingleton<IIdPermanenceManager>(x => new IdPermanenceManager(configuration));

            services.AddSingleton<HealthCheckStatuses>();

            this.ServiceProvider = services.BuildServiceProvider();

            // Migrate the database to the latest version during application startup.
            var context = this.ServiceProvider.GetRequiredService<EnergyDataHolderDatabaseContext>();
            var loggerFactory = this.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("UnitTests");
            var healthCheckStatuses = this.ServiceProvider.GetRequiredService<HealthCheckStatuses>();

            loggerFactory.AddSerilog();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Seed the database using the sample data JSON.
            var seedDataFilePath = configuration.GetValue<string>("SeedData:FilePath");
            var seedDataOverwrite = configuration.GetValue<bool>("SeedData:OverwriteExistingData", false);
            var offsetDates = configuration.GetValue<bool>("SeedData:OffsetDates", true);

            if (!string.IsNullOrEmpty(seedDataFilePath))
            {
                logger.LogInformation("Seed data file found within configuration.  Attempting to seed the repository from the seed data...");
                Task.Run(() => context.SeedDatabaseFromJsonFile(seedDataFilePath, logger, healthCheckStatuses, seedDataOverwrite, offsetDates)).Wait();
            }
        }
    }
}
