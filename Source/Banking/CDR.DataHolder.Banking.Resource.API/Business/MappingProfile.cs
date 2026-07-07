using AutoMapper;
using CDR.DataHolder.Banking.Domain.Entities;
using CDR.DataHolder.Banking.Domain.ValueObjects;
using CDR.DataHolder.Banking.Resource.API.Business.Models;
using CDR.DataHolder.Banking.Resource.API.Business.Responses;
using CDR.DataHolder.Shared.Business.Models;

namespace CDR.DataHolder.Banking.Resource.API.Business
{
    public class MappingProfile : Profile
    {
        private const int AutoMapperMaxDepth = 32;

        public MappingProfile()
        {
            CreateMap<RequestAccountTransactions, AccountTransactionsFilter>().MaxDepth(AutoMapperMaxDepth);

            CreateMap<AccountTransaction, AccountTransaction>().MaxDepth(AutoMapperMaxDepth);

            CreateMap<AccountTransaction, AccountTransactionModel>()
                .ForMember(d => d.Type, s => s.MapFrom(src => src.TransactionType))
                .ForMember(d => d.Amount, s => s.MapFrom(src => src.Amount.ToString("F2")))
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<AccountTransaction[], AccountTransactionsCollectionModel>()
                .ForMember(d => d.Transactions, s => s.MapFrom(src => src))
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Shared.Domain.ValueObjects.Page<AccountTransaction[]>, PageModel<AccountTransactionsCollectionModel>>()
                .ForMember(d => d.Meta, s => s.MapFrom(src => new MetaPaginated { TotalRecords = src.TotalRecords, TotalPages = src.TotalPages }))
                .ForMember(d => d.Data, s => s.MapFrom(src => src.Data))
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap(typeof(Shared.Domain.ValueObjects.Page<>), typeof(MetaPaginated))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Account, BankingAccountV2>()
               .ForMember(dest => dest.MaskedNumber, source => source.MapFrom(source => source.MaskedName))
               .ForMember(dest => dest.CreationDate, source => source.MapFrom(source =>
                   source.CreationDate.HasValue ? source.CreationDate.Value.ToString("yyyy-MM-dd") : null))
               .ForMember(dest => dest.IsOwned, source => source.MapFrom(source => true))
               .ReverseMap()
               .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Shared.Domain.ValueObjects.Page<Account[]>, ResponseBankingAccountListV2>()
                .ForPath(dest => dest.Data.Accounts, source => source.MapFrom(source => source.Data))
                .ForMember(dest => dest.Meta, source => source.MapFrom(source => source))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);
        }
    }
}
