using CDR.DataHolder.Banking.Repository.Infrastructure;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using CDR.DataHolder.Shared.Domain.Extensions;
using CDR.DataHolder.Shared.Repository;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                services.AddAutoMapper(cfg => { }, typeof(Startup).Assembly, typeof(BankingDataHolderDatabaseContext).Assembly);
            }

            if (industry.IsEnergy())
            {
                services.AddDbContext<EnergyDataHolderDatabaseContext>(options => options.UseSqlServer(configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.Default)));
                services.AddScoped<IIndustryDbContext, EnergyDataHolderDatabaseContext>();
                services.AddAutoMapper(cfg => { }, typeof(Startup).Assembly, typeof(EnergyDataHolderDatabaseContext).Assembly);
            }
        }
    }
}
