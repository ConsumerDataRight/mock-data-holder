using System.Collections.Generic;
using System.Linq;

namespace CDR.DataHolder.Banking.Resource.API.Business.Models
{
	public class BankingAccounts
	{
        public IEnumerable<BankingAccount> Accounts { get; set; } = Enumerable.Empty<BankingAccount>();
    }
}
