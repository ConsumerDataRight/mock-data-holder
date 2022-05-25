using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.Middleware;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Repository;
using Serilog;
using System.IO;
using CDR.DataHolder.API.Infrastructure.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Linq;
using System;

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

            string connStr = Configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Logging);
            int seedDataTimeSpan = Configuration.GetValue<int>("SeedData:TimeSpan");
            services.AddHealthChecks()
                    .AddCheck("sql-connection", () => {
					    using (var db = new SqlConnection(connStr))
					    {
						    try
						    {
                                db.Open();
                            }
                            catch (SqlException)
						    {
							    return HealthCheckResult.Unhealthy();
						    }
					    }
					    return HealthCheckResult.Healthy();
				    })
                    .AddCheck("seed-data", () => {
                        using (var db = new SqlConnection(connStr))
                        {
                            try
                            {
                                db.Open();
                                using var selectCommand = new SqlCommand($"SELECT [TimeStamp] FROM [LogEventsManageAPI] WHERE [Message] = @msg ORDER BY [TimeStamp] DESC", db);
                                selectCommand.Parameters.AddWithValue("@msg", "Hosting started");
                                var hostStarted = selectCommand.ExecuteScalar();
                                if (hostStarted != null)
                                {
                                    // Return the TimeStamp for when the Host is Started (all startup processing has completed)
                                    // use this as the reference to test if the below operations are within the appsetting - SeedData:TimeSpan value
                                    var hostStartedTimeStamp = Convert.ToDateTime(hostStarted);

                                    // IF the Seed Data was to be Imported or to be Updated
                                    // if the log record message "Seed-Data:imported" exists and the process was completed within the SeedData:TimeSpan
                                    // then it is considered to be Healthy
                                    using var selectCommand1 = new SqlCommand($"SELECT TOP 1 [Id], [TimeStamp] FROM [LogEventsManageAPI] WHERE [Message] = @msg AND [SourceContext] = @srcContext ORDER BY [TimeStamp] DESC", db);
                                    selectCommand1.Parameters.AddWithValue("@msg", "Seed-Data:imported");
                                    selectCommand1.Parameters.AddWithValue("@srcContext", "CDR.DataHolder.Manage.API.Startup");
                                    var dbReader1 = selectCommand1.ExecuteReader();
                                    if (dbReader1.HasRows)
                                    {
                                        using (dbReader1)
                                        {
                                            while (dbReader1.Read())
                                            {
                                                TimeSpan timespan = (hostStartedTimeStamp - Convert.ToDateTime(dbReader1.GetDateTime(1)));
                                                if (timespan.Minutes < seedDataTimeSpan)
                                                {
                                                    return HealthCheckResult.Healthy();
                                                }
                                            }
                                        }
                                    }

                                    dbReader1.Close();

                                    // IF the Seed Data was flagged as to NOT be Imported
                                    // if the log record message "Seed-Data:not-imported" exists and the process was completed within the SeedData:TimeSpan
                                    // then it is considered to be Healthy
                                    using var selectCommand2 = new SqlCommand($"SELECT TOP 1 [Id], [TimeStamp] FROM [LogEventsManageAPI] WHERE [Message] = @msg AND [SourceContext] = @srcContext ORDER BY [TimeStamp] DESC", db);
                                    selectCommand2.Parameters.AddWithValue("@msg", "Seed-Data:not-imported");
                                    selectCommand2.Parameters.AddWithValue("@srcContext", "CDR.DataHolder.Manage.API.Startup");
                                    var dbReader2 = selectCommand2.ExecuteReader();
                                    if (dbReader2.HasRows)
                                    {
                                        using (dbReader2)
                                        {
                                            while (dbReader2.Read())
                                            {
                                                TimeSpan timespan = (hostStartedTimeStamp - Convert.ToDateTime(dbReader2.GetDateTime(1)));
                                                if (timespan.Minutes < seedDataTimeSpan)
                                                    return HealthCheckResult.Healthy();
                                            }
                                        }
                                    }
                                }
                            }
                            catch (SqlException)
                            {
                                return HealthCheckResult.Unhealthy();
                            }

                            return HealthCheckResult.Unhealthy();
                        }
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
            EnsureDatabase(app, logger);
        }

        private void EnsureDatabase(IApplicationBuilder app, ILogger<Startup> logger)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {               
                const string HEALTHCHECK_READY_FILENAME = "_healthcheck_ready"; // MJS - Should be using ASPNet health check, not a file
                File.Delete(HEALTHCHECK_READY_FILENAME);

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
                    Task.Run(() => context.SeedDatabaseFromJsonFile(seedDataFilePath, logger, seedDataOverwrite, offsetDates)).Wait();
                }
                
                File.WriteAllText(HEALTHCHECK_READY_FILENAME, "");  // Create file to indicate MDH is ready, this can be used by Docker/Dockercompose health checks // MJS - Should be using ASPNet health check, not a file
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
                status = healthReport.Entries.Select(e => new {
                    key = e.Key,
                    value = e.Value.Status.ToString()
                })
            });
            return context.Response.WriteAsync(result);
        }
    }
}