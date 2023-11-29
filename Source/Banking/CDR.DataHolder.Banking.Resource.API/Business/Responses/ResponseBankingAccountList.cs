using CDR.DataHolder.Banking.Resource.API.Business.Models;

namespace CDR.DataHolder.Banking.Resource.API.Business.Responses
{
    public class ResponseBankingAccountList
	{
		public BankingAccounts Data { get; set; } = new BankingAccounts();
		public Shared.Business.Models.Links Links { get; set; } = new Shared.Business.Models.Links();
		public Shared.Business.Models.MetaPaginated Meta { get; set; } = new Shared.Business.Models.MetaPaginated();
	}
}
