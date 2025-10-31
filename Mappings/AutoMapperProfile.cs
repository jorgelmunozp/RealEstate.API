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
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // PROPERTY
            CreateMap<PropertyModel, PropertyDto>()
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // el Id de Mongo no se toca

            // OWNER
            // Model -> DTO: Id → IdOwner
            CreateMap<OwnerModel, OwnerDto>()
                .ForMember(dest => dest.IdOwner, opt => opt.MapFrom(src => src.Id));
            // DTO -> Model: NO toques el Id, se queda el de Mongo
            CreateMap<OwnerDto, OwnerModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // PROPERTY IMAGE
            // Model -> DTO: Id → IdPropertyImage
            CreateMap<PropertyImageModel, PropertyImageDto>()
                .ForMember(dest => dest.IdPropertyImage, opt => opt.MapFrom(src => src.Id));
            // DTO -> Model: NO toques el Id
            CreateMap<PropertyImageDto, PropertyImageModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // PROPERTY TRACE
            // Model -> DTO: Id → IdPropertyTrace
            CreateMap<PropertyTraceModel, PropertyTraceDto>()
                .ForMember(dest => dest.IdPropertyTrace, opt => opt.MapFrom(src => src.Id));
            // DTO -> Model: NO toques el Id
            CreateMap<PropertyTraceDto, PropertyTraceModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // USER
            CreateMap<UserModel, UserDto>()
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Password)));

            // AUTH / LOGIN
            CreateMap<LoginDto, UserModel>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password));
        }
    }
}
