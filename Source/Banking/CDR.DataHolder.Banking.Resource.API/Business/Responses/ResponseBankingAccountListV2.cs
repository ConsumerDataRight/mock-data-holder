using CDR.DataHolder.Banking.Resource.API.Business.Models;

namespace CDR.DataHolder.Banking.Resource.API.Business.Responses
{
    public class ResponseBankingAccountListV2
    {
        public BankingAccountsV2 Data { get; set; } = new BankingAccountsV2();

        public Shared.Business.Models.Links Links { get; set; } = new Shared.Business.Models.Links();

        public Shared.Business.Models.MetaPaginated Meta { get; set; } = new Shared.Business.Models.MetaPaginated();
    }
}
