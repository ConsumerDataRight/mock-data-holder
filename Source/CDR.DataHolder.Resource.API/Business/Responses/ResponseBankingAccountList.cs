using CDR.DataHolder.Resource.API.Business.Models;

namespace CDR.DataHolder.Resource.API.Business.Responses
{
    public class ResponseBankingAccountList
	{
		public BankingAccounts Data { get; set; }
		public Links Links { get; set; } = new Links();
		public MetaPaginated Meta { get; set; } = new MetaPaginated();
	}
}
