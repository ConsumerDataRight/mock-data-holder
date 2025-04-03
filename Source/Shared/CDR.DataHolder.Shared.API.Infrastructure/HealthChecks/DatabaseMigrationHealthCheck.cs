using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CDR.DataHolder.Shared.API.Infrastructure.HealthChecks
{
    public class DatabaseMigrationHealthCheck : IHealthCheck
    {
        private readonly IIndustryDbContext _dbContext;
        private readonly HealthCheckStatuses _healthCheckStatuses;

        public DatabaseMigrationHealthCheck(IIndustryDbContext dbContext, HealthCheckStatuses healthCheckStatuses)
        {
            _dbContext = dbContext;
            _healthCheckStatuses = healthCheckStatuses;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var migrationPending = await ((DbContext)_dbContext).Database.GetPendingMigrationsAsync(cancellationToken);

                if (!migrationPending.Any())
                {
                    _healthCheckStatuses.IsMigrationDone = true;
                    return HealthCheckResult.Healthy("Database migration completed.");
                }

                _healthCheckStatuses.IsMigrationDone = false;
                return HealthCheckResult.Unhealthy($"Database migration pending. {migrationPending.Count()} pending migrations.");
            }
            catch (Exception e)
            {
                _healthCheckStatuses.IsMigrationDone = false;
                return HealthCheckResult.Unhealthy($"Error. {e.Message}");
            }
        }
    }
}
