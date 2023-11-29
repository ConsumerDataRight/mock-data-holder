using System.Collections.Generic;
using System.Linq;

namespace CDR.DataHolder.Energy.Resource.API.Business.Models
{
    public class EnergyAccounts<T> where T : class
	{
        public IEnumerable<T> Accounts { get; set; } = Enumerable.Empty<T>();
    }
}