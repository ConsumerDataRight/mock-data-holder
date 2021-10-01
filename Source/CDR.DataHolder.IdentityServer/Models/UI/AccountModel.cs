namespace CDR.DataHolder.IdentityServer.Models.UI
{
	public class AccountModel
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string MaskedName { get; set; }
		public bool IsSelected { get; set; }
		public bool IsValid { get; set; }
		public string Type { get; internal set; }
	}
}
