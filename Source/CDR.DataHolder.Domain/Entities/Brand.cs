using System;

namespace CDR.DataHolder.Domain.Entities
{
	public class Brand
	{
		public Guid BrandId { get; set; }
		public string BrandName { get; set; }
		public string LogoUri { get; set; }

		public string Status { get; set; }

		public SoftwareProduct[] SoftwareProducts { get; set; }
		public bool IsActive
		{
			get
			{
				return Status == Statuses.Active;
			}
		}

		public LegalEntity LegalEntity { get; set; }

		public static class Statuses
		{
			public const string Active = "ACTIVE";
		}
	}
}