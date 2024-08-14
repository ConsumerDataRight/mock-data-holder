using AutoMapper;
using CDR.DataHolder.Common.API.Extensions;
using CDR.DataHolder.Common.Resource.API.Business.Models;
using CDR.DataHolder.Common.Resource.API.Business.Responses;

namespace CDR.DataHolder.Common.API.Business
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Mappings for banking industry
            CreateMap<Banking.Domain.Entities.Person, CommonPerson>();
            CreateMap<Banking.Domain.Entities.Organisation, CommonOrganisation>();

            // Mapping from Person to CustomerModel
            CreateMap<Banking.Domain.Entities.Person, CustomerModel>()
                .ForMember(dest => dest.Person, source => source.MapFrom(source => source))
                .ForMember(dest => dest.Organisation, opt => opt.Ignore());

            // Mapping from Organisation to CustomerModel
            CreateMap<Banking.Domain.Entities.Organisation, CustomerModel>()
                .ForMember(dest => dest.Organisation, source => source.MapFrom(source => source))
                .ForMember(dest => dest.Person, opt => opt.Ignore());

            // Mapping from Person to ResponseCommonCustomer
            CreateMap<Banking.Domain.Entities.Person, ResponseCommonCustomer>()
                .IgnoreAllMembers()
                .ForMember(dest => dest.Data, source => source.MapFrom(source => source));

            // Mapping from Organisation to ResponseCommonCustomer
            CreateMap<Banking.Domain.Entities.Organisation, ResponseCommonCustomer>()
                .IgnoreAllMembers()
                .ForMember(dest => dest.Data, source => source.MapFrom(source => source));

            //Mappings for energy industry
            CreateMap<Energy.Domain.Entities.Person, CommonPerson>();
            CreateMap<Energy.Domain.Entities.Organisation, CommonOrganisation>();

            // Mapping from Person to CustomerModel
            CreateMap<Energy.Domain.Entities.Person, CustomerModel>()
                .ForMember(dest => dest.Person, source => source.MapFrom(source => source))
                .ForMember(dest => dest.Organisation, opt => opt.Ignore());

            // Mapping from Organisation to CustomerModel
            CreateMap<Energy.Domain.Entities.Organisation, CustomerModel>()
                .ForMember(dest => dest.Organisation, source => source.MapFrom(source => source))
                .ForMember(dest => dest.Person, opt => opt.Ignore());

            // Mapping from Person to ResponseCommonCustomer
            CreateMap<Energy.Domain.Entities.Person, ResponseCommonCustomer>()
                .IgnoreAllMembers()
                .ForMember(dest => dest.Data, source => source.MapFrom(source => source));

            // Mapping from Organisation to ResponseCommonCustomer
            CreateMap<Energy.Domain.Entities.Organisation, ResponseCommonCustomer>()
                .IgnoreAllMembers()
                .ForMember(dest => dest.Data, source => source.MapFrom(source => source));
        }

    }
}
