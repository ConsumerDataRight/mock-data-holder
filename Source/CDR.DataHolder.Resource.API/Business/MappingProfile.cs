using AutoMapper;
using CDR.DataHolder.Domain.Entities;
using CDR.DataHolder.Domain.ValueObjects;
using CDR.DataHolder.Resource.API.Business.Models;
using CDR.DataHolder.Resource.API.Business.Responses;

namespace CDR.DataHolder.Resource.API.Business
{
    public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<RequestAccountTransactions, AccountTransactionsFilter>();

			CreateMap<AccountTransaction, AccountTransactionModel>()
				.ForMember(d => d.Type, s => s.MapFrom(src => src.TransactionType))
				.ForMember(d => d.Amount, s => s.MapFrom(src => src.Amount.ToString("F2")));

			CreateMap<AccountTransaction[], AccountTransactionsCollectionModel>()
				.ForMember(d => d.Transactions, s => s.MapFrom(src => src));

			CreateMap<Page<AccountTransaction[]>, ResponseAccountTransactions>()
				.ForMember(d => d.Meta, s => s.MapFrom(src => new MetaPaginated { TotalRecords = src.TotalRecords, TotalPages = src.TotalPages }));

			CreateMap(typeof(Page<>), typeof(MetaPaginated))
				.ReverseMap();

			CreateMap<Person, CommonPerson>()
				.ReverseMap();

			CreateMap<Organisation, CommonOrganisation>()
				.ReverseMap();

			CreateMap<Person, CustomerModel>()
				.ForMember(dest => dest.Person, source => source.MapFrom(source => source))
				.ReverseMap();

			CreateMap<Organisation, CustomerModel>()
				.ForMember(dest => dest.Organisation, source => source.MapFrom(source => source))
				.ReverseMap();

			CreateMap<Person, ResponseCommonCustomer>()
				.ForMember(dest => dest.Data, source => source.MapFrom(source => source))
				.ReverseMap();

			CreateMap<Organisation, ResponseCommonCustomer>()
				.ForMember(dest => dest.Data, source => source.MapFrom(source => source))
				.ReverseMap();

			CreateMap<Account, BankingAccount>()
				.ForMember(dest => dest.MaskedNumber, source => source.MapFrom(source => source.MaskedName))
				.ForMember(dest => dest.CreationDate, 
					source => source.MapFrom(source => source.CreationDate.HasValue ? source.CreationDate.Value.ToString("yyyy-MM-dd") : null))
				.ForMember(dest => dest.IsOwned, source => source.MapFrom(source => true))
				.ReverseMap();

			CreateMap<Page<Account[]>, ResponseBankingAccountList>()
				.ForPath(dest => dest.Data.Accounts, source => source.MapFrom(source => source.Data))
				.ForMember(dest => dest.Meta, source => source.MapFrom(source => source))
				.ReverseMap();
		}
	}
}
