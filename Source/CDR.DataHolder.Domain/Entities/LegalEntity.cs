using System;

namespace CDR.DataHolder.Domain.Entities
{
	public class LegalEntity
	{
		public Guid LegalEntityId { get; set; }
		public string LegalEntityName { get; set; }
		public string Industry { get; set; }
		public string Status { get; set; }
		public string LogoUri { get; set; }
		public Brand[] Brands { get; set; }
		public bool IsActive
		{
			get
			{
				return Status == Statuses.Active;
			}
		}

		public static class Statuses
		{
			public static string Active = "ACTIVE";
		}
	}
}