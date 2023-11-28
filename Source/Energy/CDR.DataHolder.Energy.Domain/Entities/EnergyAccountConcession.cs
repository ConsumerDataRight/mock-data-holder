namespace CDR.DataHolder.Energy.Domain.Entities
{
	public class EnergyAccountConcession
	{
		public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AdditionalInfo { get; set; }
		public string? AdditionalInfoUri { get; set; }
		public string? StartDate { get; set; }
		public string? EndDate { get; set; }
		public string? DiscountFrequency { get; set; }
		public string? Amount { get; set; }
		public string? Percentage { get; set; }
		public string[]? AppliedTo { get; set; }
	}
}