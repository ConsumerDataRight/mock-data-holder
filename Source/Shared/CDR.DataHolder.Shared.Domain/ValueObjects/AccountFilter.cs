namespace CDR.DataHolder.Shared.Domain.ValueObjects
{
	public class AccountFilter
	{
		public AccountFilter(string[] allowedAccountIds)
		{
			AllowedAccountIds = allowedAccountIds;
		}		
		public bool? IsOwned { get; set; }
		public string? ProductCategory { get; set; }
		public string? OpenStatus { get; set; }
		public string[] AllowedAccountIds { get; set; }
	}
}
