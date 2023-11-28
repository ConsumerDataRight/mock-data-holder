using CDR.DataHolder.Banking.Resource.API.Business.Models;
using CDR.DataHolder.Shared.Business.Models;
using System.Threading.Tasks;

namespace CDR.DataHolder.Banking.Resource.API.Business.Services
{
    public interface ITransactionsService
    {
        Task<PageModel<AccountTransactionsCollectionModel>> GetAccountTransactions(RequestAccountTransactions request, int page, int pageSize);
    }
}
