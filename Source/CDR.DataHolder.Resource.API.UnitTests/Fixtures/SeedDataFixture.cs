using System;
using System.IO;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Repository;
using CDR.DataHolder.Repository.Infrastructure;
using CDR.DataHolder.Resource.API.Business.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CDR.DataHolder.Resource.API.UnitTests.Fixtures
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
            services.AddDbContext<DataHolderDatabaseContext>(options => options.UseSqlServer(connectionStr));

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            services.AddAutoMapper(typeof(Startup), typeof(DataHolderDatabaseContext));

            services.AddScoped<IResourceRepository, ResourceRepository>();
            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddScoped<ITransactionsService, TransactionsService>();
            services.AddSingleton<IIdPermanenceManager>(x => new IdPermanenceManager(configuration));

            this.ServiceProvider = services.BuildServiceProvider();

            // Migrate the database to the latest version during application startup.
            var context = this.ServiceProvider.GetRequiredService<DataHolderDatabaseContext>();
            var loggerFactory = this.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("UnitTests");

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
                Task.Run(() => context.SeedDatabaseFromJsonFile(seedDataFilePath, logger, seedDataOverwrite, offsetDates)).Wait();
            }
        }
    }
}
