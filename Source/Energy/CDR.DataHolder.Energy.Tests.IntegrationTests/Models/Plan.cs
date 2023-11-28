namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class Plan
    {
        public string PlanId { get; set; } = null!;
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? DisplayName { get; set; }
        public string Type { get; set; } = null!;
        public string FuelType { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string BrandName { get; set; } = null!;
        public string? ApplicationUri { get; set; }
        public string? CustomerType { get; set; }
    }
}
