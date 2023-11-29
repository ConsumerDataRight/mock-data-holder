using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CDR.DataHolder.Shared.API.Infrastructure.HealthChecks
{
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly IIndustryDbContext _dbContext;

        public SqlServerHealthCheck(IIndustryDbContext dbContext)
        {
            _dbContext=dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if( await ((DbContext)_dbContext).Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Healthy("SQL Server connection successful");
                }

                return HealthCheckResult.Unhealthy("Cannot connect to SQL Server");
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy($"Error. {e.Message}");
            }
        }
    }
}
