using CDR.DataHolder.Banking.Repository.Infrastructure;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using CDR.DataHolder.Shared.Repository;
using Microsoft.EntityFrameworkCore;
using CDR.DataHolder.Banking.Domain.Repositories;
using CDR.DataHolder.Banking.Repository;
using CDR.DataHolder.Energy.Domain.Repositories;
using CDR.DataHolder.Energy.Repository;
using CDR.DataHolder.Shared.API.Infrastructure.Authorisation;
using CDR.DataHolder.Shared.API.Infrastructure.Authorization;
using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.Business.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using static CDR.DataHolder.Shared.API.Infrastructure.Constants;
using CDR.DataHolder.Shared.API.Infrastructure.Exceptions;
using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
using static CDR.DataHolder.Shared.Domain.Constants;
using CDR.DataHolder.Shared.Domain.Extensions;

namespace CDR.DataHolder.Common.API.Infrastructure
{
    public static class ServiceCollectionExtension
    {
        public static void AddIndustryDBContext(this IServiceCollection services, IConfiguration configuration)
        {
            var industry = configuration.GetValue<string>("Industry") ?? Industry.Banking;
            string defaultConnectionString = configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default) 
                ?? throw new InvalidOperationException($"{nameof(defaultConnectionString)} is not set");

            if (industry.IsBanking())
            {
                services.AddScoped<IBankingResourceRepository, BankingResourceRepository>();
                services.AddScoped<IIndustryDbContext, BankingDataHolderDatabaseContext>();
                services.AddDbContext<BankingDataHolderDatabaseContext>(options => options.UseSqlServer(defaultConnectionString));
                services.AddAutoMapper(typeof(Program), typeof(BankingDataHolderDatabaseContext));
            }
            else if (industry.IsEnergy())
            {
                services.AddScoped<IEnergyResourceRepository, EnergyResourceRepository>();
                services.AddDbContext<EnergyDataHolderDatabaseContext>(options => options.UseSqlServer(defaultConnectionString));
                services.AddScoped<IIndustryDbContext, EnergyDataHolderDatabaseContext>();
                services.AddAutoMapper(typeof(Program), typeof(EnergyDataHolderDatabaseContext));
            }
        }

        public static ICommonRepository GetCommonRepository(this IServiceProvider serviceProvider, string industry)
        {
            ICommonRepository? commonRepository;

            if (industry.IsBanking())
            {
                commonRepository = serviceProvider.GetService<IBankingResourceRepository>();
            }
            else if (industry.IsEnergy())
            {
                commonRepository = serviceProvider.GetService<IEnergyResourceRepository>();
            }
            else
            {
                throw new InvalidIndustryException();
            }
           

            if (commonRepository == null)
            {
                throw new RepositoryException("Resource repository cannot not be resolved.");
            }

            return commonRepository;
        }

        public static void AddAuthenticationAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IIdPermanenceManager, IdPermanenceManager>();

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
                    policy.Requirements.Add(new ScopeRequirement(ApiScopes.Common.CustomerBasicRead, identityServerIssuer));
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
    }
}
