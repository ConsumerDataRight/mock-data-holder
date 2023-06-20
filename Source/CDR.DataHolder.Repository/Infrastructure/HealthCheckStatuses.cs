namespace CDR.DataHolder.Repository.Infrastructure
{
    public enum SeedingStatus
    {
        NotStarted,
        Succeeded,
        Failed,
        NotConfigured
    }

    public enum AppStatus
    {
        Started,
        Shutdown,
        NotStarted
    }
    public class HealthCheckStatuses
    {
        public bool IsMigrationDone { get; set; } = false;

        public SeedingStatus SeedingStatus { get; set; } = SeedingStatus.NotStarted;

        public AppStatus AppStatus { get; set; } = AppStatus.NotStarted;
    }
}
