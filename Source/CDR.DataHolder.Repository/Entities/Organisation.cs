using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Repository.Entities
{
	public class Organisation
	{
		[Key]
		public Guid OrganisationId { get; set; }

		[MaxLength(100)]
		public string AgentFirstName { get; set; }

		[Required, MaxLength(100)]
		public string AgentLastName { get; set; }

		[Required, MaxLength(100)]
		public string AgentRole { get; set; }

		[Required, MaxLength(500)]
		public string BusinessName { get; set; }

		[MaxLength(500)]
		public string LegalName { get; set; }

		[MaxLength(100)]
		public string ShortName { get; set; }

		[MaxLength(11)]
		public string Abn { get; set; }

		[MaxLength(9)]
		public string Acn { get; set; }

		public bool? IsAcnCRegistered { get; set; }

		[MaxLength(10)]
		public string IndustryCode { get; set; }

		public string IndustryCodeVersion { get; set; }

		public string OrganisationType { get; set; }

		[MaxLength(3)]
		public string RegisteredCountry { get; set; }

		public DateTime? EstablishmentDate { get; set; }

		public DateTime? LastUpdateTime { get; set; }

		public virtual Customer Customer { get; set; }
	}
}