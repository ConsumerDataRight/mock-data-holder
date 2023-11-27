using CDR.DataHolder.Shared.Repository.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace CDR.DataHolder.Shared.API.Infrastructure.HealthChecks
{
    public class DatabaseSeedingHealthCheck : IHealthCheck
    {
        private readonly HealthCheckStatuses _healthCheckStatuses;

        public DatabaseSeedingHealthCheck(HealthCheckStatuses healthCheckStatuses)
        {
            _healthCheckStatuses=healthCheckStatuses;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {           
            if (_healthCheckStatuses.SeedingStatus == SeedingStatus.NotStarted)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Database seeding not started"));
            }

            if (_healthCheckStatuses.SeedingStatus == SeedingStatus.NotConfigured)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Database seeding not configured"));
            }

            if (_healthCheckStatuses.SeedingStatus == SeedingStatus.Failed)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Database seeding failed"));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Database seeding successful"));
        }
    }
}
