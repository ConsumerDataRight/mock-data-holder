using System;

namespace CDR.DataHolder.Domain.Entities
{
	public class Organisation : Customer
	{
		private DateTime? lastUpdateTime;

		public Guid OrganisationId { get; set; }
		public string AgentFirstName { get; set; }
		public string AgentLastName { get; set; }
		public string AgentRole { get; set; }
		public string BusinessName { get; set; }
		public string LegalName { get; set; }
		public string ShortName { get; set; }
		public string Abn { get; set; }
		public string Acn { get; set; }
		public bool? IsACNCRegistered { get; set; }
		public string IndustryCode { get; set; }
		public string IndustryCodeVersion { get; set; }
		public string OrganisationType { get; set; }
		public string RegisteredCountry { get; set; }
		public string EstablishmentDate { get; set; }
		public DateTime? LastUpdateTime { get => lastUpdateTime == null ? lastUpdateTime : lastUpdateTime.Value.ToUniversalTime(); set => lastUpdateTime = value; }
	}
}
