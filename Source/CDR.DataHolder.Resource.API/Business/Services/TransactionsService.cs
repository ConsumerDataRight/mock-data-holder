using System.Threading.Tasks;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Domain.ValueObjects;
using CDR.DataHolder.Resource.API.Business.Models;
using CDR.DataHolder.Resource.API.Business.Responses;

namespace CDR.DataHolder.Resource.API.Business.Services
{
    public class TransactionsService: ITransactionsService
    {
        private readonly IResourceRepository _resourceRepository;
        private readonly AutoMapper.IMapper _mapper;

        public TransactionsService(IResourceRepository resourceRepository, AutoMapper.IMapper mapper)
        {
            _resourceRepository = resourceRepository;
            _mapper = mapper;
        }

        public async Task<ResponseAccountTransactions> GetAccountTransactions(RequestAccountTransactions request, int page, int pageSize)
        {
            var filters = _mapper.Map<AccountTransactionsFilter>(request);
            var results = await _resourceRepository.GetAccountTransactions(filters, page, pageSize);
            return _mapper.Map<ResponseAccountTransactions>(results);
        }
    }
}
