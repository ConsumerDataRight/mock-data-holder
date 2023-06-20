using CDR.DataHolder.API.Infrastructure.Filters;
using CDR.DataHolder.API.Infrastructure.HealthChecks;
using CDR.DataHolder.API.Infrastructure.Middleware;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Repository;
using CDR.DataHolder.Repository.Infrastructure;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Manage.API
{
    public class Startup
    {        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = ModelStateErrorMiddleware.ExecuteResult;
                });

            // This is to manage the EF database context through the web API DI.
            // If this is to be done inside the repository project itself, we need to manage the context life-cycle explicitly.
            services.AddDbContext<DataHolderDatabaseContext>(options => options.UseSqlServer(Configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default)));

            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddAutoMapper(typeof(Startup), typeof(DataHolderDatabaseContext));
            services.AddScoped<LogActionEntryAttribute>();
            services.AddSingleton<HealthCheckStatuses>();

            services.AddHealthChecks()
                    .AddCheck<ApplicationHealthCheck>("Application Check", HealthStatus.Unhealthy, new[] { "AppStatus" }, TimeSpan.FromSeconds(3))
                    .AddCheck<SqlServerHealthCheck>("SQL Server Check", HealthStatus.Unhealthy, new[] { "DatabaseStatus" }, TimeSpan.FromSeconds(10))
                    .AddCheck<DatabaseMigrationHealthCheck>("Migrations Check", HealthStatus.Unhealthy, new[] { "Migrations" }, TimeSpan.FromSeconds(10))
                    .AddCheck<DatabaseSeedingHealthCheck>("Seeding Check", HealthStatus.Unhealthy, new[] { "Seeding" }, TimeSpan.FromSeconds(3));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IHostApplicationLifetime applicationLifetime, HealthCheckStatuses healthStatuses)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            applicationLifetime.ApplicationStarted.Register(() => { healthStatuses.AppStatus = AppStatus.Started;});
            applicationLifetime.ApplicationStopping.Register(() => { healthStatuses.AppStatus = AppStatus.Shutdown;});

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
            EnsureDatabase(app, logger, healthStatuses);
        }

        private void EnsureDatabase(IApplicationBuilder app, ILogger<Startup> logger, HealthCheckStatuses healthCheckStatuses)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                if (RunMigrations())
                {
                    // Use DBO connection string since it has DBO rights needed to update db schema
                    var optionsBuilder = new DbContextOptionsBuilder<DataHolderDatabaseContext>();
                    optionsBuilder.UseSqlServer(Configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Migrations)
                        ?? throw new System.Exception($"Connection string '{DbConstants.ConnectionStringNames.Resource.Migrations}' not found"));

                    // Ensure the database is created and up to date.
                    using var dbMigrationsContext = new DataHolderDatabaseContext(optionsBuilder.Options);
                    dbMigrationsContext.Database.Migrate();
                }

                // Seed the database using the sample data JSON.
                var seedDataFilePath = Configuration.GetValue<string>("SeedData:FilePath");
                var seedDataOverwrite = Configuration.GetValue<bool>("SeedData:OverwriteExistingData", false);
                var offsetDates = Configuration.GetValue<bool>("SeedData:OffsetDates", true);

                if (!string.IsNullOrEmpty(seedDataFilePath))
                {
                    var context = serviceScope.ServiceProvider.GetRequiredService<DataHolderDatabaseContext>();
                    logger.LogInformation("Seed data file found within configuration.  Attempting to seed the repository from the seed data...");
                    Task.Run(() => context.SeedDatabaseFromJsonFile(seedDataFilePath, logger, healthCheckStatuses, seedDataOverwrite, offsetDates)).Wait();
                }
                else
                {
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