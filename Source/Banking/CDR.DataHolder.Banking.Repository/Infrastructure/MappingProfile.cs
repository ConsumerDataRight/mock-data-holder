using AutoMapper;
using CDR.DataHolder.Banking.Repository.Entities;
using DomainEntities = CDR.DataHolder.Banking.Domain.Entities;

namespace CDR.DataHolder.Banking.Repository.Infrastructure
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
                .ForMember(
                    dest => dest.MiddleNames,
                    source => source.MapFrom(source => string.IsNullOrEmpty(source.MiddleNames) ? null : source.MiddleNames.Split(',', System.StringSplitOptions.TrimEntries)))
                .ForMember(dest => dest.Accounts, source => source.MapFrom(s => s.Customer.Accounts))
                .ForMember(dest => dest.CustomerId, source => source.MapFrom(s => s.Customer.CustomerId))
                .ForMember(dest => dest.LoginId, source => source.MapFrom(s => s.Customer.LoginId))
                .ForMember(dest => dest.CustomerUType, source => source.MapFrom(s => s.Customer.CustomerUType))
                .ReverseMap();

            CreateMap<Organisation, DomainEntities.Organisation>()
                .ForMember(
                    dest => dest.EstablishmentDate,
                    source => source.MapFrom(source => source.EstablishmentDate == null ? null : source.EstablishmentDate.Value.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.Accounts, source => source.MapFrom(s => s.Customer.Accounts))
                .ForMember(dest => dest.CustomerId, source => source.MapFrom(s => s.Customer.CustomerId))
                .ForMember(dest => dest.LoginId, source => source.MapFrom(s => s.Customer.LoginId))
                .ForMember(dest => dest.CustomerUType, source => source.MapFrom(s => s.Customer.CustomerUType))
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
