using CDR.DataHolder.API.Infrastructure.Authorisation;
using CDR.DataHolder.API.Infrastructure.Authorization;
using CDR.DataHolder.API.Infrastructure.Filters;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.API.Infrastructure.Middleware;
using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.API.Logger;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.IdentityServer.ClientAuthentication;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Formatters;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Middleware;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using CDR.DataHolder.IdentityServer.Stores;
using CDR.DataHolder.IdentityServer.Validation;
using CDR.DataHolder.Repository;
using CDR.DataHolder.Repository.Infrastructure;
using FluentValidation;
using IdentityServer4.Configuration;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.Extensions;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace CDR.DataHolder.IdentityServer
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Initialise configuration settings.
            var configurationSettings = new ConfigurationSettings(_configuration);
            services.AddSingleton<IConfigurationSettings>(configurationSettings);
            services.AddAutoMapper(typeof(Startup), typeof(DataHolderDatabaseContext));

            services.AddHttpClient<IJwksService, JwksService>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (a, b, c, d) => true
                    };
                    return handler;
                });

            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IJwtTokenCreationService, JwtTokenCreationService>();
            services.AddScoped<AuthoriseErrorHandlingMiddleware>();
            services.AddScoped<UserInfoErrorHandlingMiddleware>();
            services.AddScoped<ICustomGrantService, CustomGrantService>();
            services.AddScoped<IAuthorizeRequestUriService, AuthorizeRequestUriService>();
            services.AddSingleton<ISecurityService, SecurityService>();

            services.AddScoped<IIdSvrRepository, IdSvrRepository>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddSingleton<IIdPermanenceManager, IdPermanenceManager>();
            services.AddSingleton<IRevokedTokenStore, RevokedTokenStore>();

            services.AddScoped<Validation.IIntrospectionRequestValidator, IntrospectionRequestValidator>();

            var connectionStr = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default);

            services.AddDbContext<DataHolderDatabaseContext>(options => options.UseSqlServer(connectionStr));

            string migrationsConnectionString = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Identity.Migrations);
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            var issuerUri = _configuration[Constants.ConfigurationKeys.IssuerUri];

            services.AddIdentityServer(options =>
            {
                options.IssuerUri = issuerUri;
                options.MutualTls.Enabled = false; // override the default Ids4 mechanism for Mtls

                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.Discovery = ConfigureDiscoveryOptions(_configuration);
                options.Endpoints = ConfigureEndpoints();

                options.InputLengthRestrictions.Scope = 10000;

                options.UserInteraction.LoginUrl = "/account/login";
                options.UserInteraction.LoginReturnUrlParameter = "returnUrl";
            })
                .AddSecretParser<ClientSecretParser>()
                .AddSecretValidator<Validation.ClientSecretValidator>()
                .AddPersistedGrantStore<CdrPersistedGrantStore>()
                .AddClientStore<ClientStore>()
                .AddCustomAuthorizeRequestValidator<CustomAuthorizeRequestValidator>()
                .AddCustomTokenRequestValidator<CustomTokenRequestValidator>()
                .AddCertificateSigningCredentials(_configuration)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b => b.UseSqlServer(migrationsConnectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b => b.UseSqlServer(migrationsConnectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                .AddInMemoryIdentityResources(InMemoryConfig.IdentityResources)
                .AddInMemoryApiResources(InMemoryConfig.Apis(_configuration))
                .AddInMemoryApiScopes(InMemoryConfig.ApiScopes(_configuration))
                .AddProfileService<ProfileService>();

            services.AddDbContext<PersistedGrantDbContext>(options => options.UseSqlServer(_configuration.GetConnectionString(DbConstants.ConnectionStringNames.Identity.Default)));
            services.AddDbContext<DataHolderDatabaseContext>(options => options.UseSqlServer(_configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default)));

            // override original IdentityServer validator because it doesn't follow CDS specs (redirect_uri should not be required)
            services.AddTransient<ITokenResponseGenerator, Services.TokenResponseGenerator>();

            // override original identityServer validator to handle non-supported token type hint
            services.AddTransient<ITokenRevocationRequestValidator, CustomTokenRevocationRequestValidator>();

            // override original identityServer revocation response generator
            services.AddTransient<ITokenRevocationResponseGenerator, CustomTokenRevocationResponseGenerator>();

            services.AddTransient<IResourceValidator, CustomResourceValidator>();

            services.AddTransient<IUserInfoRequestValidator, CustomUserInfoRequestValidator>();

            services.AddScoped<IClaimsService, ClaimsService>();

            services.AddScoped<IPushedAuthorizationRequestValidator, PushedAuthorizationRequestValidator>();
            services.AddScoped<IPushedAuthorizationRequestService, PushedAuthorizationRequestService>();

            services.AddScoped<IJwtRequestUriHttpClient, CustomJwtRequestUriHttpClient>();

            services.AddScoped<IClientArrangementRevocationEndpointRequestService, ClientArrangementRevocationEndpointRequestService>();
            services.AddHttpClient<IClientArrangementRevocationEndpointHttpClient, ClientArrangementRevocationEndpointHttpClient>();

            services.AddScoped<ITokenResponseGenerator, Services.TokenResponseGenerator>();
            services.AddScoped<IClientStore, ClientStore>();
            services.AddScoped<DynamicClientStore>();
            services.AddScoped<IResourceRepository, ResourceRepository>();
            services.AddScoped<IStatusRepository, StatusRepository>();

            services.AddTransient<ITokenCreationService, JweTokenCreationService>();
            services.AddScoped<IJwtTokenCreationService, JwtTokenCreationService>();
            services.AddScoped<IDiscoveryResponseGenerator, CustomDiscoveryResponseGenerator>();
            services.AddTransient<IRefreshTokenService, RefreshTokenService>();
            services.AddTransient<ITokenService, CustomTokenService>();

            services.AddTransient<CustomJwtRequestValidator>();

            services.AddScoped<IIdSvrService, IdSvrService>();

            services.AddRouting();
            services.AddControllersWithViews(options =>
            {
                // Requiring a specialised JWT formatter, as core uses JSON by default.
                options.InputFormatters.Clear();
                options.InputFormatters.Add(new JwtInputFormatter());
            })
            .AddRazorRuntimeCompilation()
            .ConfigureApiBehaviorOptions(x =>
            {
                // MVC implicit validation (via [ApiController]) circuit breaks our ability to handle
                // FluentValidation results, and won't allow to 'massage' the response.
                // We are suprassing it so that we can capture results with an ActionFilter.
                x.SuppressModelStateInvalidFilter = true;
            });

            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ApiVersionReader = new HeaderApiVersionReader("x-v");
            });

            // if the distributed cache connection string has been set then use it, otherwise fall back to in-memory caching.
            if (UseDistributedCache())
            {
                services.AddStackExchangeRedisCache(options => {
                    options.Configuration = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache.Default);
                    options.InstanceName = "dataholder-banking-cache-";
                });

                services.AddDataProtection()
                    .SetApplicationName("mdh-idsvr")
                    .PersistKeysToStackExchangeRedis(
                        StackExchange.Redis.ConnectionMultiplexer.Connect(_configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache.Default)),
                        "dataholder-banking-cache-dp-keys");
            }
            else
            {
                // Use in memory cache.
                services.AddDistributedMemoryCache();
            }

            services.AddSession(o =>
            {
                o.Cookie.Name = "mdh-idsvr";
                o.Cookie.SameSite = SameSiteMode.None;
                o.Cookie.HttpOnly = true;
                o.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            services.AddTransient<ITokenReplayCache, TokenReplayCache>();

            AddValidators(services);

            // Enable authentication and authorisation
            AddAuthenticationAuthorization(services, _configuration);

            services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

            services.AddScoped<LogActionEntryAttribute>();

            if (_configuration.GetSection("SerilogRequestResponseLogger") != null)
            {
                Log.Logger.Information("Adding request response logging middleware");
                services.AddRequestResponseLogging();
            }

        }

        private bool UseDistributedCache()
        {
            var cacheConnectionString = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Cache.Default);
            return !string.IsNullOrEmpty(cacheConnectionString);
        }

        private static void AddAuthenticationAuthorization(IServiceCollection services, IConfiguration configuration)
        {
            var issuerUri = configuration.GetValue<string>("IssuerUri");

            services.AddHttpContextAccessor();

            services
                .AddAuthentication(config =>
                {
                    config.DefaultScheme = "bearer-or-cookie";
                })
                .AddPolicyScheme("bearer-or-cookie", "Bearer or Cookie", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var bearerAuth = context.Request.Headers["Authorization"].FirstOrDefault()?.StartsWith("Bearer ") ?? false;
                        if (bearerAuth)
                            return JwtBearerDefaults.AuthenticationScheme;
                        else
                            return "idsvr";
                    };
                })
                .AddCookie("idsvr", options => 
                { 
                    // We don't want the login session to stick around in the server. After entering the user credentials (username, otp, etc), user has
                    // 60 seconds to agree to the consent. You can change it based on the time it takes to complete your consent flow.
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(60);
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var securityService = new SecurityService(configuration);

                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuerUri,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = false,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        IssuerSigningKeys = securityService.SigningKeys,
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
                options.AddPolicy(AuthorisationPolicy.DynamicClientRegistration.ToString(), policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.Requirements.Add(new MtlsRequirement());
                    policy.Requirements.Add(new ScopeRequirement("cdr:registration", issuerUri));
                });
            });

            services.AddSingleton<IAuthorizationHandler, ScopeHandler>();
            services.AddSingleton<IAuthorizationHandler, MtlsHandler>();
            services.AddSingleton<IAuthorizationHandler, AccessTokenHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration, ILogger<Startup> logger)
        {
            app.UseSerilogRequestLogging();
            app.UseMiddleware<RequestResponseLoggingMiddleware>();


            var basePath = configuration.GetValue<string>(Constants.ConfigurationKeys.BasePath, "");
            if (!string.IsNullOrEmpty(basePath))
            {
                logger.LogInformation("Using base path: {basePath}", basePath);
                app.UsePathBase(basePath);
            }

            // This is to set the IDSVR base uri during redirects. Or else, it will the localhost which is not ideal when hosted.
            app.Use(async (ctx, next) =>
            {
                var baseUri = configuration.GetBaseUri();
                if (!string.IsNullOrEmpty(baseUri))
                {
                    logger.LogInformation("Using base uri: {baseUri}", baseUri);
                    ctx.SetIdentityServerOrigin(baseUri);
                }

                await next();
            });

            // ExceptionHandlingMiddleware must be first in the line, so it will catch all unhandled exceptions.
            app.UseMiddleware<AuthoriseErrorHandlingMiddleware>();
            app.UseMiddleware<UserInfoErrorHandlingMiddleware>();
            

            // Allow sensitive data to be logged in dev environment only
            IdentityModelEventSource.ShowPII = env.IsDevelopment();

            app.UseIdentityServer();

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());

            InitializeDatabase(app);
        }

        private static DiscoveryOptions ConfigureDiscoveryOptions(IConfiguration configuration)
        {
            var jwksUri = configuration[Constants.ConfigurationKeys.JwksUri];
            var authorizationEndpoint = configuration[Constants.ConfigurationKeys.AuthorizeUri];
            var introspectionUri = configuration[Constants.ConfigurationKeys.IntrospectionUri];
            var parUri = configuration[Constants.ConfigurationKeys.ParUri];
            var registerUri = configuration[Constants.ConfigurationKeys.RegisterUri];
            var arrangementRevocationUri = configuration[Constants.ConfigurationKeys.ArrangementRevocationUri];
            var userinfoUri = configuration[Constants.ConfigurationKeys.UserinfoUri];

            var discovery = new DiscoveryOptions()
            {
                // Override default entries with custom entries to only show relevant otions for Cts.
                ShowApiScopes = true,
                ShowResponseModes = false,
                ShowIdentityScopes = true,
                ShowExtensionGrantTypes = false,
                ShowClaims = true,
                ShowResponseTypes = false,
                ShowGrantTypes = false,

                // Overridden in the CustomDiscoveryResponseGenerator.
                ShowTokenEndpointAuthenticationMethods = false,
            };

            if (!string.IsNullOrWhiteSpace(introspectionUri))
            {
                discovery.CustomEntries.Add(CdsConstants.Discovery.IntrospectionEndpoint, introspectionUri);
            }
            if (!string.IsNullOrWhiteSpace(parUri))
            {
                discovery.CustomEntries.Add(CdsConstants.Discovery.PushedAuthorizedRequestEndPoint, parUri);
            }
            if (!string.IsNullOrWhiteSpace(registerUri))
            {
                discovery.CustomEntries.Add(CdsConstants.Discovery.RegistrationEndpoint, registerUri);
            }
            if (!string.IsNullOrWhiteSpace(arrangementRevocationUri))
            {
                discovery.CustomEntries.Add(CdsConstants.Discovery.CDRArrangementRevocationEndPoint, arrangementRevocationUri);
            }
            if (!string.IsNullOrWhiteSpace(userinfoUri))
            {
                discovery.CustomEntries.Add(CdsConstants.Discovery.UserInfoEndpointOverride, userinfoUri);
            }
            if (!string.IsNullOrWhiteSpace(jwksUri))
            {
                discovery.CustomEntries.Add(CdsConstants.Discovery.JwksUriOverride, jwksUri);
            }
            if (!string.IsNullOrWhiteSpace(authorizationEndpoint))
            {
                discovery.CustomEntries.Add(CdsConstants.Discovery.AuthorizationEndpointOverride, authorizationEndpoint);
            }

            var extended = new Dictionary<string, object>
                {
                    { CdsConstants.Discovery.RequestObjectSigningAlgorithmsSupported, new string[] { CdsConstants.Algorithms.Signing.PS256, CdsConstants.Algorithms.Signing.ES256 } },
                    { CdsConstants.Discovery.IdTokenEncryptionAlgorithmsSupported, IdTokenEncryptionAlgorithms() },
                    { CdsConstants.Discovery.IdTokenEncryptionEncValuesSupported, IdTokenEncryptionEncValues() },
                    { CdsConstants.Discovery.TokenEndpointAuthSigningAlgorithmsSupported, new string[] { CdsConstants.Algorithms.Signing.PS256, CdsConstants.Algorithms.Signing.ES256 } },
                    { CdsConstants.Discovery.ResponseTypesSupported, new string[] { CdsConstants.ResponseTypes.CodeIdToken } },
                    { CdsConstants.Discovery.GrantTypesSupported, new string[] { CdsConstants.GrantTypes.AuthorizationCode, CdsConstants.GrantTypes.RefreshToken, CdsConstants.GrantTypes.ClientCredentials } },
                    { CdsConstants.Discovery.TokenEndpointAuthenticationMethodsSupported, new string[] { CdsConstants.EndpointAuthenticationMethods.PrivateKeyJwt } },
                    { CdsConstants.Discovery.TlsClientCertificateBoundAccessTokens, true },
                    { CdsConstants.Discovery.ClaimsParameterSupported, true },
                    { CdsConstants.Discovery.ResponseModesSupported , new string[] { CdsConstants.ResponseModes.FormPost, CdsConstants.ResponseModes.Fragment } },
                };

            foreach (var entry in extended)
            {
                discovery.CustomEntries.Add(entry.Key, entry.Value);
            }

            return discovery;
        }

        private static string[] IdTokenEncryptionAlgorithms()
        {
            return new string[]
            {
                CdsConstants.Algorithms.Jwe.Alg.RSAOAEP,
                CdsConstants.Algorithms.Jwe.Alg.RSAOAEP256,
                CdsConstants.Algorithms.Jwe.Alg.RSA15,
            };
        }

        private static string[] IdTokenEncryptionEncValues()
        {
            return new string[]
            {
                CdsConstants.Algorithms.Jwe.Enc.A128CBCHS256,
                CdsConstants.Algorithms.Jwe.Enc.A192CBCHS384,
                CdsConstants.Algorithms.Jwe.Enc.A256CBCHS512,
                CdsConstants.Algorithms.Jwe.Enc.A128GCM,
                CdsConstants.Algorithms.Jwe.Enc.A192GCM,
                CdsConstants.Algorithms.Jwe.Enc.A256GCM,
            };
        }

        private static EndpointsOptions ConfigureEndpoints()
        {
            return new EndpointsOptions
            {
                EnableDiscoveryEndpoint = true,
                EnableTokenEndpoint = true,
                EnableAuthorizeEndpoint = true,
                EnableTokenRevocationEndpoint = true,
                EnableCheckSessionEndpoint = false,
                EnableDeviceAuthorizationEndpoint = false,
                EnableEndSessionEndpoint = false,
                EnableIntrospectionEndpoint = false,
                EnableJwtRequestUri = true,
                EnableUserInfoEndpoint = true,
            };
        }

        private static void InitializeDatabase(IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();

            // Note: you can also use the below commands to manually do db migrations:
            // dotnet ef database update --context IdentityServer4.EntityFramework.DbContexts.ConfigurationDbContext
            // dotnet ef database update --context IdentityServer4.EntityFramework.DbContexts.PersistedGrantDbContext
            var persistedGrantDbContext = serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
            persistedGrantDbContext.Database.Migrate();            

            var configurationDbContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            configurationDbContext.Database.Migrate();
        }

        private static void AddValidators(IServiceCollection services)
        {
            services.AddTransient<IClientRegistrationRequestValidator, ClientRegistrationRequestValidator>();
            services.AddTransient<IValidator<ClientTokenRequest>, ClientTokenRequestValidator>();
            services.AddTransient<IValidator<ClientRevocationRequest>, ClientRevocationRequestValidator>();
            services.AddTransient<IValidator<ClientArrangementRevocationRequest>, ClientArrangementRevocationRequestValidator>();
            services.AddTransient<IValidator<ClientDetails>, ClientDetailsValidator>();
            services.AddTransient<IValidator<MtlsCredential>, MtlsCredentialValidator>();
        }
    }
}