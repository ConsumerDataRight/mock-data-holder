namespace CDR.DataHolder.Banking.Resource.API.Business.Models
{
	public class BankingAccount
	{
        public string AccountId { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public string? OpenStatus { get; set; }
        public bool IsOwned { get; set; }
        public string MaskedNumber { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }
}
