using System.Threading.Tasks;
using CDR.DataHolder.Resource.API.Business.Models;
using CDR.DataHolder.Resource.API.Business.Responses;

namespace CDR.DataHolder.Resource.API.Business.Services
{
    public interface ITransactionsService
    {
        Task<ResponseAccountTransactions> GetAccountTransactions(RequestAccountTransactions request, int page, int pageSize);
    }
}
