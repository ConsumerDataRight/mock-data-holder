using CDR.DataHolder.Energy.Domain.Repositories;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using CDR.DataHolder.Shared.API.Infrastructure.Authorisation;
using CDR.DataHolder.Shared.API.Infrastructure.Authorization;
using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
using CDR.DataHolder.Shared.API.Infrastructure.Filters;
using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using CDR.DataHolder.Shared.API.Infrastructure.Middleware;
using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.API.Infrastructure.Versioning;
using CDR.DataHolder.Shared.API.Logger;
using CDR.DataHolder.Shared.Business;
using CDR.DataHolder.Shared.Business.Middleware;
using CDR.DataHolder.Shared.Business.Services;
using CDR.DataHolder.Shared.Domain.Repositories;
using CDR.DataHolder.Shared.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using DbConstants = CDR.DataHolder.Shared.Repository.DbConstants;
using ResourceRepository = CDR.DataHolder.Energy.Repository.EnergyResourceRepository;

namespace CDR.DataHolder.Energy.Resource.API
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
            services.AddScoped<IEnergyResourceRepository, ResourceRepository>();
            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddScoped<ResourceAuthoriseErrorHandlingMiddleware>();
            services.AddSingleton<IIdPermanenceManager, IdPermanenceManager>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mock Data Holder Discovery API", Version = "v1" });
            });

            services.AddSwaggerGenNewtonsoftSupport();
            services.AddMvc().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });

            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = ModelStateErrorMiddleware.ExecuteResult;
                });

            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;                
                options.ApiVersionSelector = new ApiVersionSelector(options);
                options.ErrorResponses = new ErrorResponseVersion();
            });

            // This is to manage the EF database context through the web API DI.
            // If this is to be done inside the repository project itself, we need to manage the context life-cycle explicitly.
            services.AddDbContext<EnergyDataHolderDatabaseContext>(options => options.UseSqlServer(Configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default)));

            // Enable authentication and authorisation
            AddAuthenticationAuthorization(services, Configuration);

            services.AddAutoMapper(typeof(Startup), typeof(EnergyDataHolderDatabaseContext));
            services.AddScoped<LogActionEntryAttribute>();

            if (Configuration.GetSection("SerilogRequestResponseLogger") != null)
            {
                Log.Logger.Information("Adding request response logging middleware");
                services.AddRequestResponseLogging();
            }
        }

        private static void AddAuthenticationAuthorization(IServiceCollection services, IConfiguration configuration)
        {
            var identityServerUrl = configuration.GetValue<string>("IdentityServerUrl");
            var identityServerIssuer = configuration.GetValue<string>("IdentityServerIssuerUri") ?? string.Empty;

            services.AddHttpContextAccessor();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = identityServerUrl;
                options.RequireHttpsMetadata = true;
                options.Audience = "cds-au";
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ClockSkew =TimeSpan.FromMinutes(1),
                    RequireAudience = true,
                    RequireExpirationTime = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                };

                var handler = new HttpClientHandler();
                handler.SetServerCertificateValidation(configuration);
                options.BackchannelHttpHandler = handler;
            });

            // Authorization
            services.AddMvcCore().AddAuthorization(options =>
            {
                options.AddPolicy(AuthorisationPolicy.GetCustomersApi.ToString(), policy =>
                {
                    policy.Requirements.Add(new ScopeRequirement(CDR.DataHolder.Shared.API.Infrastructure.Constants.ApiScopes.Common.CustomerBasicRead, identityServerIssuer));
                    policy.Requirements.Add(new MtlsRequirement());
                    policy.Requirements.Add(new AccessTokenRequirement());
                });

                options.AddPolicy(AuthorisationPolicy.GetAccountsApi.ToString(), policy =>
                {
                    policy.Requirements.Add(new ScopeRequirement(CDR.DataHolder.Shared.API.Infrastructure.Constants.ApiScopes.Energy.AccountsBasicRead, identityServerIssuer));
                    policy.Requirements.Add(new MtlsRequirement());
                    policy.Requirements.Add(new AccessTokenRequirement());
                });

                options.AddPolicy(AuthorisationPolicy.GetConcessionsApi.ToString(), policy =>
                {
                    policy.Requirements.Add(new ScopeRequirement(CDR.DataHolder.Shared.API.Infrastructure.Constants.ApiScopes.Energy.ConcessionsRead, identityServerIssuer));
                    policy.Requirements.Add(new MtlsRequirement());
                    policy.Requirements.Add(new AccessTokenRequirement());
                });
            });

            services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
            services.AddSingleton<IAuthorizationHandler, MtlsHandler>();
            services.AddSingleton<IAuthorizationHandler, AccessTokenHandler>();
            services.AddScoped<IClaimsTransformation, TokenTransformService>();

            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Please enter into field the word 'Bearer' following by space and JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Scheme = "Bearer",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new List<string>()
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();
            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            // ExceptionHandlingMiddleware must be first in the line, so it will catch all unhandled exceptions.
            app.UseMiddleware<ResourceAuthoriseErrorHandlingMiddleware>();
            
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context => await ApiExceptionHandler.Handle(context));
            });

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mock Data Holder Discovery API v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Add custom middleware
            app.UseInteractionId();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
