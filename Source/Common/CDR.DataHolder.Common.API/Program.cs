using System.Text.Json.Serialization;
using AutoMapper;
using CDR.DataHolder.Common.API.Infrastructure;
using CDR.DataHolder.Shared.API.Infrastructure.Extensions;
using CDR.DataHolder.Shared.API.Infrastructure.Filters;
using CDR.DataHolder.Shared.API.Infrastructure.Middleware;
using CDR.DataHolder.Shared.API.Infrastructure.Versioning;
using CDR.DataHolder.Shared.Business;
using Serilog;
using static CDR.DataHolder.Shared.Domain.Constants;

var builder = WebApplication.CreateBuilder(args);

var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

// Get the value of the "industry" key from appsettings.json
var industry = config.GetValue<string>("Industry") ?? Industry.Banking;

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.{industry}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProcessId()
    .Enrich.WithProcessName()
    .Enrich.WithThreadId()
    .Enrich.WithThreadName()
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Release")
    .CreateLogger();

builder.Logging.ClearProviders();

builder.Logging.AddSerilog(logger);

builder.Services.ConfigureWebServer(builder.Configuration, logger);

// Add services to the container.
builder.Services.AddScoped<LogActionEntryAttribute>();

builder.Services.AddIndustryDBContext(builder.Configuration); // includes adding automapper, as it needs to register the industry-specific dbcontext

builder.Services.AddScoped<ICommonRepositoryFactory, CommonRepositoryFactory>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddCdrApiVersioning();

builder.Services.AddAuthenticationAuthorization(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context => await ApiExceptionHandler.Handle(context));
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseInteractionId();

// assert Automapper configuration is valid.
app.Services.GetService<IMapper>()?.ConfigurationProvider.AssertConfigurationIsValid();

if (app.Environment.IsDevelopment())
{
    app.UseCdrSwagger();
}

app.MapControllers();

await app.RunAsync();
