using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CDR.DataHolder.API.Infrastructure.HealthChecks
{
    public class DatabaseMigrationHealthCheck : IHealthCheck
    {
        private readonly DataHolderDatabaseContext _dbContext;
        private readonly HealthCheckStatuses _healthCheckStatuses;

        public DatabaseMigrationHealthCheck(DataHolderDatabaseContext dbContext, HealthCheckStatuses healthCheckStatuses)
        {
            _dbContext = dbContext;
            _healthCheckStatuses=healthCheckStatuses;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var migrationPending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

                if (migrationPending.Count() == 0)
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
