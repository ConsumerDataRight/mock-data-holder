using System;

namespace CDR.DataHolder.Domain.ValueObjects
{
	public class AccountFilter
	{
		public AccountFilter(string[] allowedAccountIds)
		{
			AllowedAccountIds = allowedAccountIds;
		}
		public Guid? CustomerId { get; set; }
		public bool? IsOwned { get; set; }
		public string ProductCategory { get; set; }
		public string OpenStatus { get; set; }
		public string[] AllowedAccountIds { get; set; }
	}
}
