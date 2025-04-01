using CDR.DataHolder.Manage.API.Infrastructure;
using CDR.DataHolder.Shared.API.Infrastructure.Filters;
using CDR.DataHolder.Shared.API.Infrastructure.HealthChecks;
using CDR.DataHolder.Shared.API.Infrastructure.Middleware;
using CDR.DataHolder.Shared.Domain.Repositories;
using CDR.DataHolder.Shared.Repository;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Manage.API
{
    public class Startup
    {
        private readonly string _industry;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _industry = configuration.GetValue<string>("Industry") ?? string.Empty;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = ModelStateErrorMiddleware.ExecuteResult;
                });

            services.AddIndustryDBContext(Configuration);

            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddScoped<LogActionEntryAttribute>();
            services.AddSingleton<HealthCheckStatuses>();
            services.AddScoped<IndustryDbContextFactory>();

            services.AddHealthChecks()
                    .AddCheck<ApplicationHealthCheck>("Application Check", HealthStatus.Unhealthy, new[] { "AppStatus" }, TimeSpan.FromSeconds(3))
                    .AddTypeActivatedCheck<SqlServerHealthCheck>($"{_industry} SQLServer", HealthStatus.Unhealthy, new[] { "DatabaseStatus" }, TimeSpan.FromSeconds(10))
                    .AddCheck<DatabaseMigrationHealthCheck>("Migrations Check", HealthStatus.Unhealthy, new[] { "Migrations" }, TimeSpan.FromSeconds(10))
                    .AddCheck<DatabaseSeedingHealthCheck>("Seeding Check", HealthStatus.Unhealthy, new[] { "Seeding" }, TimeSpan.FromSeconds(3));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IHostApplicationLifetime applicationLifetime, HealthCheckStatuses healthStatuses, IWebHostEnvironment webHostEnvironment)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            applicationLifetime.ApplicationStarted.Register(() => { healthStatuses.AppStatus = AppStatus.Started; });
            applicationLifetime.ApplicationStopping.Register(() => { healthStatuses.AppStatus = AppStatus.Shutdown; });

            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                ResponseWriter = CustomResponseWriter
            });

            // Ensure the database exists and is up to the latest version.
            EnsureDatabase(app, logger, healthStatuses, webHostEnvironment).Wait();
        }

        private async Task EnsureDatabase(IApplicationBuilder app, ILogger<Startup> logger, HealthCheckStatuses healthCheckStatuses, IWebHostEnvironment webHostEnvironment)
        {
            logger.LogInformation("DataHolder is being configured for the industry of {Industry}", _industry);

            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var dbContextFactory = serviceScope.ServiceProvider.GetRequiredService<IndustryDbContextFactory>();
                if (RunMigrations())
                {
                    IIndustryDbContext industryMigrationDbContext = dbContextFactory.Create(_industry, DbConstants.ConnectionStringType.Migrations);
                    logger.LogInformation("Running migrations");
                    var dbContext = industryMigrationDbContext as DbContext;
                    await dbContext!.Database.MigrateAsync().ConfigureAwait(false);
                }

                // Configure logger with the DB
                Program.ConfigureSerilog(Configuration, true);

                // Seed the database using the sample data JSON.
                var seedFilePath = Configuration.GetValue<string>("SeedData:FilePath");
                var seedDataOverwrite = Configuration.GetValue<bool>("SeedData:OverwriteExistingData", false);
                var offsetDates = Configuration.GetValue<bool>("SeedData:OffsetDates", true);

                if (!string.IsNullOrEmpty(seedFilePath))
                {
                    logger.LogInformation("Seed data file found within configuration, and the industry is {Industry}. Attempting to seed the repository from the seed data...", _industry);

                    IIndustryDbContext industryDbContext = dbContextFactory.Create(_industry, DbConstants.ConnectionStringType.Default);
                    var dbContext = industryDbContext as DbContext;
                    await dbContext!.SeedDatabaseFromJsonFile(Path.Combine(webHostEnvironment.ContentRootPath, seedFilePath), logger, healthCheckStatuses, seedDataOverwrite, offsetDates).ConfigureAwait(false);
                }
                else
                {
                    logger.LogInformation("Seed data file {SeedFilePath} not configured for {Industry}", seedFilePath, _industry);
                    healthCheckStatuses.SeedingStatus = SeedingStatus.NotConfigured;
                }
            }
        }

        /// <summary>
        /// Determine if EF Migrations should run.
        /// </summary>
        private bool RunMigrations()
        {
            // Run migrations if the DBO connection string is set.
            var dbo = Configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Migrations);
            return !string.IsNullOrEmpty(dbo);
        }

        private static Task CustomResponseWriter(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = "application/json";
            var result = JsonConvert.SerializeObject(new
            {
                status = healthReport.Entries.Select(e => new
                {
                    key = e.Key,
                    value = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            });
            return context.Response.WriteAsync(result);
        }
    }
}
