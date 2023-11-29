namespace CDR.DataHolder.Energy.Resource.API.Business.Models
{
	public class EnergyConcession
	{
		public string Type { get; set; } = string.Empty;
		public string DisplayName { get; set; } = string.Empty;
        public string AdditionalInfo { get; set; } = string.Empty;
        public string? AdditionalInfoUri { get; set; }
		public string? StartDate { get; set; }
		public string? EndDate { get; set; }
		public string DiscountFrequency { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Percentage { get; set; } = string.Empty;
        public string[]? AppliedTo { get; set; }
	}
}
