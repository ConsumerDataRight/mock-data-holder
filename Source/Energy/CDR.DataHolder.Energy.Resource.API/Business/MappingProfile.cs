using System;
using System.Linq;
using AutoMapper;
using CDR.DataHolder.Energy.Domain.Entities;
using CDR.DataHolder.Energy.Domain.ValueObjects;
using CDR.DataHolder.Energy.Resource.API.Business.Responses;
using CDR.DataHolder.Shared.Business.Models;
using CDR.DataHolder.Shared.Domain.ValueObjects;

namespace CDR.DataHolder.Energy.Resource.API.Business
{
    public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Models.RequestAccountConsessions, AccountConsessionsFilter>();

			CreateMap(typeof(Page<>), typeof(MetaPaginated))
				.ReverseMap();

			CreateMap<Energy.Domain.Entities.EnergyAccountPlan, Models.EnergyAccountPlan>()
				.ForMember(dest => dest.ServicePointIds, source => source.MapFrom(source => 
					source.ServicePoints == null ? Array.Empty<string>() : source.ServicePoints.Select(sp => sp.ServicePointId)))
				.ReverseMap();
			CreateMap<Energy.Domain.Entities.EnergyPlanOverview, Models.EnergyPlanOverview>()
				.ReverseMap();

            CreateMap<Energy.Domain.Entities.EnergyAccount, Models.BaseEnergyAccount>()
                .ForMember(dest => dest.CreationDate, source => source.MapFrom(source => 
					source.CreationDate == null? string.Empty : source.CreationDate.Value.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.Plans, source => source.MapFrom(source => source.Plans))
                .ReverseMap();
            CreateMap<Energy.Domain.Entities.EnergyAccount, Models.EnergyAccount>()
                .IncludeBase<Energy.Domain.Entities.EnergyAccount, Models.BaseEnergyAccount>()
                .ReverseMap();
            CreateMap<Energy.Domain.Entities.EnergyAccount, Models.EnergyAccountV2>()
				.IncludeBase<Energy.Domain.Entities.EnergyAccount, Models.BaseEnergyAccount>()
                .ForMember(dest => dest.OpenStatus, source => source.MapFrom(source => source.OpenStatus))
                .ReverseMap();
            CreateMap<Page<Energy.Domain.Entities.EnergyAccount[]>, EnergyAccountListResponse<Models.EnergyAccount>>()
				.ForPath(dest => dest.Data.Accounts, source => source.MapFrom(source => source.Data))
				.ForMember(dest => dest.Meta, source => source.MapFrom(source => source))
				.ReverseMap();
            CreateMap<Page<Energy.Domain.Entities.EnergyAccount[]>, EnergyAccountListResponse<Models.EnergyAccountV2>>()
                .ForPath(dest => dest.Data.Accounts, source => source.MapFrom(source => source.Data))
                .ForMember(dest => dest.Meta, source => source.MapFrom(source => source))
                .ReverseMap();

            CreateMap<EnergyAccountConcession[], EnergyConcessionsResponse>()
				.ForPath(dest => dest.Data.Concessions, source => source.MapFrom(source => source));
			CreateMap<EnergyAccountConcession, Models.EnergyConcession>()
				.ReverseMap();
		}
	}
}
