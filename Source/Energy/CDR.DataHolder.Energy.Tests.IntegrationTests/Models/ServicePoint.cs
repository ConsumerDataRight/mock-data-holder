namespace CDR.DataHolder.Energy.Tests.IntegrationTests.Models
{
    public class ServicePoint
    {
        public string ServicePointId { get; set; } = null!;

        public string NationalMeteringId { get; set; } = null!;

        public string ServicePointClassification { get; set; } = null!;

        public string ServicePointStatus { get; set; } = null!;

        public string JurisdictionCode { get; set; } = null!;

        public bool IsGenerator { get; set; }

        public string? ValidFromDate { get; set; }

        public DateTime LastUpdateDateTime { get; set; }
    }
}
