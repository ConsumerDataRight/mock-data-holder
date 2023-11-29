using System;

namespace CDR.DataHolder.Banking.Resource.API.Business.Models
{
    public class AccountTransactionsCollectionModel
    {
        public AccountTransactionModel[] Transactions { get; set; } = Array.Empty<AccountTransactionModel>();
    }
}
