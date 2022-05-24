using System;

namespace CDR.DataHolder.Domain.Entities
{
	public class SoftwareProduct
	{
		public Guid SoftwareProductId { get; set; }
		public string SoftwareProductName { get; set; }
		public string SoftwareProductDescription { get; set; }
		public string LogoUri { get; set; }
		public string Status { get; set; }
		public bool IsActive
		{
			get
			{
				return Status == Statuses.Active;
			}
		}

		public Brand Brand { get; set; }

		public static class Statuses
		{
			public const string Active = "ACTIVE";
		}
	}
}