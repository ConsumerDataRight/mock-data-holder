using AutoMapper;
using CDR.DataHolder.Repository.Entities;
using DomainEntities = CDR.DataHolder.Domain.Entities;
namespace CDR.DataHolder.Repository.Infrastructure
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Transaction, DomainEntities.AccountTransaction>()
				.ReverseMap();

			CreateMap<Account, DomainEntities.Account>()
				.ReverseMap();

			CreateMap<Person, DomainEntities.Person>()
				.ForMember(dest => dest.MiddleNames,
					source => source.MapFrom(source => string.IsNullOrEmpty(source.MiddleNames) ? null : source.MiddleNames.Split(',', System.StringSplitOptions.TrimEntries)))
				.ReverseMap();

			CreateMap<Organisation, DomainEntities.Organisation>()
				.ForMember(dest => dest.EstablishmentDate, 
					source => source.MapFrom(source => source.EstablishmentDate == null ? null : source.EstablishmentDate.Value.ToString("yyyy-MM-dd")))
				.ReverseMap();

			CreateMap<Customer, DomainEntities.Customer>()
				.ForMember(dest => dest.Accounts, source => source.MapFrom(source => source.Accounts))
				.ReverseMap();

			CreateMap<Customer, DomainEntities.Person>()
				.IncludeMembers(source => source.Person, source => source)
				.ReverseMap();

			CreateMap<Customer, DomainEntities.Organisation>()
				.IncludeMembers(source => source.Organisation)
				.ReverseMap();

		}
	}
}
