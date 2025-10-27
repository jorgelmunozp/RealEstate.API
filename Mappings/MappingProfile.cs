using AutoMapper;

// Property
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Dto;

// Owner
using RealEstate.API.Modules.Owner.Model;
using RealEstate.API.Modules.Owner.Dto;

// PropertyImage
using RealEstate.API.Modules.PropertyImage.Model;
using RealEstate.API.Modules.PropertyImage.Dto;

// PropertyTrace
using RealEstate.API.Modules.PropertyTrace.Model;
using RealEstate.API.Modules.PropertyTrace.Dto;

// User
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.User.Dto;

// Auth
using RealEstate.API.Modules.Auth.Dto;

namespace RealEstate.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Property
            CreateMap<PropertyModel, PropertyDto>()
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Owner
            CreateMap<OwnerModel, OwnerDto>()
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Property Image
            CreateMap<PropertyImageModel, PropertyImageDto>()
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Property Trace
            CreateMap<PropertyTraceModel, PropertyTraceDto>()
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // User
            CreateMap<UserModel, UserDto>()
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Password)));

            // Auth / Login
            CreateMap<LoginDto, UserModel>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password));
        }
    }
}
