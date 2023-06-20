using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace CDR.DataHolder.API.Infrastructure.HealthChecks
{
    public class ApplicationHealthCheck : IHealthCheck
    {
        private readonly HealthCheckStatuses _healthCheckStatuses;

        public ApplicationHealthCheck(HealthCheckStatuses healthCheckStatuses)
        {
            _healthCheckStatuses = healthCheckStatuses;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_healthCheckStatuses.AppStatus == AppStatus.NotStarted)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Application not started"));
            }

            if (_healthCheckStatuses.AppStatus == AppStatus.Shutdown)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Application is shutdown"));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Application started successfully"));
        }
    }
}
