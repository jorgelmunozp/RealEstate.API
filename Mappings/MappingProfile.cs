using AutoMapper;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Owner.Model;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.PropertyImage.Model;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyTrace.Model;
using RealEstate.API.Modules.PropertyTrace.Dto;
namespace RealEstate.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<PropertyModel, PropertyDto>();
            CreateMap<OwnerModel, OwnerDto>();
            CreateMap<PropertyImageModel, PropertyImageDto>();
            CreateMap<PropertyTraceModel, PropertyTraceDto>();
        }
    }
}
