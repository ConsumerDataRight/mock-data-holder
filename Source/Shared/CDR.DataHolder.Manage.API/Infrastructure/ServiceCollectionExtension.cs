using CDR.DataHolder.Banking.Repository.Infrastructure;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using CDR.DataHolder.Shared.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using CDR.DataHolder.Shared.Domain.Extensions;

namespace CDR.DataHolder.Manage.API.Infrastructure
{
    public static class ServiceCollectionExtension
    {
        public static void AddIndustryDBContext(this IServiceCollection services, IConfiguration configuration)
        {
            var industry = configuration.GetValue<string>("Industry") ?? string.Empty;

            if (industry.IsBanking())
            {
                services.AddScoped<IIndustryDbContext, BankingDataHolderDatabaseContext>();
                services.AddDbContext<BankingDataHolderDatabaseContext>(options => options.UseSqlServer(configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default)));
                services.AddAutoMapper(typeof(Startup), typeof(BankingDataHolderDatabaseContext));
            }
            if (industry.IsEnergy())
            {
                services.AddDbContext<EnergyDataHolderDatabaseContext>(options => options.UseSqlServer(configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default)));
                services.AddScoped<IIndustryDbContext, EnergyDataHolderDatabaseContext>();
                services.AddAutoMapper(typeof(Startup), typeof(EnergyDataHolderDatabaseContext));
            }
        }
    }
}
