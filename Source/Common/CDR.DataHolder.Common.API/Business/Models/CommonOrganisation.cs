using System;

namespace CDR.DataHolder.Common.Resource.API.Business.Models
{
	public class CommonOrganisation
	{
        public DateTime? LastUpdateTime { get; set; }
        public string? AgentFirstName { get; set; }
        public string AgentLastName { get; set; } = string.Empty;
        public string AgentRole { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? ShortName { get; set; }
        public string? Abn { get; set; }
        public string? Acn { get; set; }
        public bool? IsACNCRegistered { get; set; }
        public string? IndustryCode { get; set; }
        public string? IndustryCodeVersion { get; set; }
        public string? OrganisationType { get; set; }
        public string? RegisteredCountry { get; set; }
        public string? EstablishmentDate { get; set; }
    }
}
