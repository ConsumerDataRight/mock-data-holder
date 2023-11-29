using CDR.DataHolder.Banking.Resource.API.Business.Models;
using CDR.DataHolder.Banking.Domain.Repositories;
using CDR.DataHolder.Banking.Domain.ValueObjects;
using System.Threading.Tasks;
using CDR.DataHolder.Shared.Business.Models;

namespace CDR.DataHolder.Banking.Resource.API.Business.Services
{
    public class TransactionsService: ITransactionsService
    {
        private readonly IBankingResourceRepository _resourceRepository;
        private readonly AutoMapper.IMapper _mapper;

        public TransactionsService(IBankingResourceRepository resourceRepository, AutoMapper.IMapper mapper)
        {
            _resourceRepository = resourceRepository;
            _mapper = mapper;
        }

        public async Task<PageModel<AccountTransactionsCollectionModel>> GetAccountTransactions(RequestAccountTransactions request, int page, int pageSize)
        {
            var filters = _mapper.Map<AccountTransactionsFilter>(request);
            var results = await _resourceRepository.GetAccountTransactions(filters, page, pageSize);
            return _mapper.Map<PageModel<AccountTransactionsCollectionModel>>(results);
        }
    }
}
