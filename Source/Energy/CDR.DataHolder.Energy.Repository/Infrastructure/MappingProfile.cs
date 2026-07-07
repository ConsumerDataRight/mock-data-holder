using System;
using AutoMapper;
using CDR.DataHolder.Energy.Repository.Entities;
using DomainEntities = CDR.DataHolder.Energy.Domain.Entities;

namespace CDR.DataHolder.Energy.Repository.Infrastructure
{
    public class MappingProfile : Profile
    {
        private const int AutoMapperMaxDepth = 32;

        public MappingProfile()
        {
            CreateMap<AccountPlan, DomainEntities.EnergyAccountPlan>()
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);
            CreateMap<AccountConcession, DomainEntities.EnergyAccountConcession>()
                .ForMember(dest => dest.StartDate, source => source.MapFrom(source => source.StartDate.HasValue ? source.StartDate.Value.ToString("yyyy-MM-dd") : null))
                .ForMember(dest => dest.EndDate, source => source.MapFrom(source => source.EndDate.HasValue ? source.EndDate.Value.ToString("yyyy-MM-dd") : null))
                .ForMember(dest => dest.Amount, source => source.MapFrom(source => string.IsNullOrEmpty(source.Amount) ? null : decimal.Parse(source.Amount).ToString("F2")))
                .ForMember(dest => dest.Percentage, source => source.MapFrom(source => string.IsNullOrEmpty(source.Percentage) ? null : decimal.Parse(source.Percentage).ToString("F2")))
                .ForMember(dest => dest.AppliedTo, source => source.MapFrom(source => string.IsNullOrEmpty(source.AppliedTo) ? Array.Empty<string>() : source.AppliedTo.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);
            CreateMap<ServicePoint, DomainEntities.EnergyServicePoint>()
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);
            CreateMap<PlanOverview, DomainEntities.EnergyPlanOverview>()
                .ForMember(dest => dest.StartDate, source => source.MapFrom(source => source.StartDate.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.EndDate, source => source.MapFrom(source => source.EndDate.HasValue ? source.EndDate.Value.ToString("yyyy-MM-dd") : null))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Account, DomainEntities.EnergyAccount>()
                .ForMember(dest => dest.Plans, source => source.MapFrom(source => source.AccountPlans))
                .ForMember(dest => dest.Concessions, source => source.MapFrom(source => source.AccountConcessions))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Person, DomainEntities.Person>()
                .ForMember(
                    dest => dest.MiddleNames,
                    source => source.MapFrom(source => string.IsNullOrEmpty(source.MiddleNames) ? null : source.MiddleNames.Split(',', System.StringSplitOptions.TrimEntries)))
                .ForMember(dest => dest.Accounts, source => source.MapFrom(s => s.Customer.Accounts))
                .ForMember(dest => dest.CustomerId, source => source.MapFrom(s => s.Customer.CustomerId))
                .ForMember(dest => dest.LoginId, source => source.MapFrom(s => s.Customer.LoginId))
                .ForMember(dest => dest.CustomerUType, source => source.MapFrom(s => s.Customer.CustomerUType))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Organisation, DomainEntities.Organisation>()
                .ForMember(
                    dest => dest.EstablishmentDate,
                    source => source.MapFrom(source => source.EstablishmentDate == null ? null : source.EstablishmentDate.Value.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.Accounts, source => source.MapFrom(s => s.Customer.Accounts))
                .ForMember(dest => dest.CustomerId, source => source.MapFrom(s => s.Customer.CustomerId))
                .ForMember(dest => dest.LoginId, source => source.MapFrom(s => s.Customer.LoginId))
                .ForMember(dest => dest.CustomerUType, source => source.MapFrom(s => s.Customer.CustomerUType))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Customer, DomainEntities.Customer>()
                .ForMember(dest => dest.Accounts, source => source.MapFrom(source => source.Accounts))
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Customer, DomainEntities.Person>()
                .IncludeMembers(source => source.Person, source => source)
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Customer, DomainEntities.Organisation>()
                .IncludeMembers(source => source.Organisation)
                .ReverseMap()
                .MaxDepth(AutoMapperMaxDepth);
        }
    }
}
