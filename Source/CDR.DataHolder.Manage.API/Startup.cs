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
            services.AddDbContext<DataHolderDatabaseContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IStatusRepository, StatusRepository>();

            services.AddAutoMapper(typeof(Startup), typeof(DataHolderDatabaseContext));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Ensure the database exists and is up to the latest version.
            EnsureDatabase(app, env, logger);
        }

        private void EnsureDatabase(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<DataHolderDatabaseContext>();

                // Ensure the database is created and up to date.
                context.Database.Migrate();

                // Seed the database using the sample data JSON.
                var seedDataFilePath = Configuration.GetValue<string>("SeedData:FilePath");
                var seedDataOverwrite = Configuration.GetValue<bool>("SeedData:OverwriteExistingData", false);
                var offsetDates = Configuration.GetValue<bool>("SeedData:OffsetDates", true);

                if (!string.IsNullOrEmpty(seedDataFilePath))
                {
                    logger.LogInformation("Seed data file found within configuration.  Attempting to seed the repository from the seed data...");
                    Task.Run(() => context.SeedDatabaseFromJsonFile(seedDataFilePath, logger, seedDataOverwrite, offsetDates)).Wait();
                }
            }
        }
    }
}
