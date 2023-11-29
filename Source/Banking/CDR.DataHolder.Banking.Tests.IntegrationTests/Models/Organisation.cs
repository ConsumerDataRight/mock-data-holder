using System;

namespace CDR.DataHolder.Banking.Tests.IntegrationTests.Models
{
    public class Organisation
    {
        public string? OrganisationID { get; set; } = null!;
        public string? AgentFirstName { get; set; }
        public string AgentLastName { get; set; } = null!;
        public string AgentRole { get; set; } = null!;
        public string BusinessName { get; set; } = null!;
        public string? LegalName { get; set; }
        public string? ShortName { get; set; }
        public string? ABN { get; set; }
        public string? ACN { get; set; }
        public string? IsACNCRegistered { get; set; }
        public string? IndustryCode { get; set; }
        public string? IndustryCodeVersion { get; set; }
        public string? OrganisationType { get; set; }
        public string? RegisteredCountry { get; set; }
        public DateTime? EstablishmentDate { get; set; }
        public DateTime? LastUpdateTime { get; set; }
    }

}
