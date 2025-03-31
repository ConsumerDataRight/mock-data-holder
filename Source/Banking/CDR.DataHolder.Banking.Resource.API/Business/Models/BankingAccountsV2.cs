using System.Collections.Generic;
using System.Linq;

namespace CDR.DataHolder.Banking.Resource.API.Business.Models
{
    public class BankingAccountsV2
    {
        public IEnumerable<BankingAccountV2> Accounts { get; set; } = Enumerable.Empty<BankingAccountV2>();
    }
}
