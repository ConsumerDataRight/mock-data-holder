using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using CDR.DataHolder.API.Infrastructure.Authorisation;
using CDR.DataHolder.API.Infrastructure.Authorization;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.API.Infrastructure.Middleware;
using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Repository;
using CDR.DataHolder.Repository.Infrastructure;
using CDR.DataHolder.Resource.API.Business;
using CDR.DataHolder.Resource.API.Business.Services;
using CDR.DataHolder.Resource.API.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;

namespace CDR.DataHolder.Resource.API
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
            services.AddScoped<IResourceRepository, ResourceRepository>();
            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddScoped<ITransactionsService, TransactionsService>();
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
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ApiVersionReader = new HeaderApiVersionReader("x-v");
                options.ErrorResponses = new ErrorResponseVersion();
            });

            // This is to manage the EF database context through the web API DI.
            // If this is to be done inside the repository project itself, we need to manage the context life-cycle explicitly.
            services.AddDbContext<DataHolderDatabaseContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            // Enable authentication and authorisation
            AddAuthenticationAuthorization(services, Configuration);

            services.AddAutoMapper(typeof(Startup), typeof(DataHolderDatabaseContext));
        }

        private void AddAuthenticationAuthorization(IServiceCollection services, IConfiguration configuration)
        {
            var identityServerUrl = configuration.GetValue<string>("IdentityServerUrl");
            var identityServerIssuer = configuration.GetValue<string>("IdentityServerIssuerUri");

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

                // Ignore server certificate issues when retrieving OIDC configuration and JWKS.
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                };
            });

            // Authorization
            services.AddMvcCore().AddAuthorization(options =>
            {
                options.AddPolicy(AuthorisationPolicy.GetCustomersApi.ToString(), policy =>
                {
                    policy.Requirements.Add(new ScopeRequirement("common:customer.basic:read", identityServerIssuer));
                    policy.Requirements.Add(new MtlsRequirement());
                    policy.Requirements.Add(new AccessTokenRequirement());
                });

                options.AddPolicy(AuthorisationPolicy.GetAccountsApi.ToString(), policy =>
                {
                    policy.Requirements.Add(new ScopeRequirement("bank:accounts.basic:read", identityServerIssuer));
                    policy.Requirements.Add(new MtlsRequirement());
                    policy.Requirements.Add(new AccessTokenRequirement());
                });

                options.AddPolicy(AuthorisationPolicy.GetTransactionsApi.ToString(), policy =>
                {
                    policy.Requirements.Add(new ScopeRequirement("bank:transactions:read", identityServerIssuer));
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // ExceptionHandlingMiddleware must be first in the line, so it will catch all unhandled exceptions.
            app.UseMiddleware<ResourceAuthoriseErrorHandlingMiddleware>();

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
