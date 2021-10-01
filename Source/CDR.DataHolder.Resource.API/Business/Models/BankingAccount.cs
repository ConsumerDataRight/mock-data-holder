namespace CDR.DataHolder.Resource.API.Business.Models
{
	public class BankingAccount
	{
        public string AccountId { get; set; }
        public string CreationDate { get; set; }
        public string DisplayName { get; set; }
        public string Nickname { get; set; }
        public string OpenStatus { get; set; }
        public bool IsOwned { get; set; }
        public string MaskedNumber { get; set; }
        public string ProductCategory { get; set; }
        public string ProductName { get; set; }
    }
}
