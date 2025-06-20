using System;
using CDR.DataHolder.Banking.Repository.Infrastructure;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using CDR.DataHolder.Shared.API.Infrastructure.Exceptions;
using CDR.DataHolder.Shared.Domain;
using CDR.DataHolder.Shared.Repository;
using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CDR.DataHolder.Manage.API.Infrastructure
{
    public class IndustryDbContextFactory
    {
        private readonly IConfiguration _configuration;

        public IndustryDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IIndustryDbContext Create(string industry, string connectionStringType)
        {
            return industry switch
            {
                Constants.Industry.Banking => CreateBankingDbContext(connectionStringType),
                Constants.Industry.Energy => CreateEnergyDbContext(connectionStringType),
                _ => throw new InvalidIndustryException(),
            };
        }

        private IIndustryDbContext CreateBankingDbContext(string connectionStringType)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BankingDataHolderDatabaseContext>();
            var bankingConnectionString = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.GetConnectionString(connectionStringType) ?? string.Empty)
                ?? throw new InvalidOperationException($"Connection string '{DbConstants.ConnectionStringNames.Resource.GetConnectionString(connectionStringType)}' not found");

            optionsBuilder.UseSqlServer(bankingConnectionString);

            return new BankingDataHolderDatabaseContext(optionsBuilder.Options);
        }

        private IIndustryDbContext CreateEnergyDbContext(string connectionStringType)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EnergyDataHolderDatabaseContext>();
            var energyConnectionString = _configuration.GetConnectionString(DbConstants.ConnectionStringNames.Resource.GetConnectionString(connectionStringType) ?? string.Empty)
                ?? throw new InvalidOperationException($"Connection string '{DbConstants.ConnectionStringNames.Resource.GetConnectionString(connectionStringType)}' not found");

            optionsBuilder.UseSqlServer(energyConnectionString);

            return new EnergyDataHolderDatabaseContext(optionsBuilder.Options);
        }
    }
}
