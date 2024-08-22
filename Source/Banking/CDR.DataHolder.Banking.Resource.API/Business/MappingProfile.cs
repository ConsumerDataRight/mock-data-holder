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
		public MappingProfile()
		{
			CreateMap<RequestAccountTransactions, AccountTransactionsFilter>();

			CreateMap<AccountTransaction, AccountTransaction>();

            CreateMap<AccountTransaction, AccountTransactionModel>()
				.ForMember(d => d.Type, s => s.MapFrom(src => src.TransactionType))
				.ForMember(d => d.Amount, s => s.MapFrom(src => src.Amount.ToString("F2")));

			CreateMap<AccountTransaction[], AccountTransactionsCollectionModel>()
				.ForMember(d => d.Transactions, s => s.MapFrom(src => src));			       


            CreateMap<Shared.Domain.ValueObjects.Page<AccountTransaction[]>, PageModel<AccountTransactionsCollectionModel>>()
				.ForMember(d => d.Meta, s => s.MapFrom(src => new MetaPaginated { TotalRecords = src.TotalRecords, TotalPages = src.TotalPages }))
				.ForMember(d => d.Data, s => s.MapFrom(src => src.Data));
            

            CreateMap(typeof(Shared.Domain.ValueObjects.Page<>), typeof(MetaPaginated))
				.ReverseMap();

            CreateMap<Account, BankingAccountV2>()
               .ForMember(dest => dest.MaskedNumber, source => source.MapFrom(source => source.MaskedName))
               .ForMember(dest => dest.CreationDate, source => source.MapFrom(source =>
                   source.CreationDate.HasValue ? source.CreationDate.Value.ToString("yyyy-MM-dd") : null))
               .ForMember(dest => dest.IsOwned, source => source.MapFrom(source => true))
               .ReverseMap();

            CreateMap<Shared.Domain.ValueObjects.Page<Account[]>, ResponseBankingAccountListV2>()
                .ForPath(dest => dest.Data.Accounts, source => source.MapFrom(source => source.Data))
                .ForMember(dest => dest.Meta, source => source.MapFrom(source => source))
                .ReverseMap();
        }
	}
}
