using AutoMapper;
using RealEstate.API.Models;
using RealEstate.API.Dtos;

namespace RealEstate.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Propiedad principal
            CreateMap<Property, PropertyDto>();

            // Subobjetos
            CreateMap<Owner, OwnerDto>();
            CreateMap<PropertyImage, PropertyImageDto>();
            CreateMap<PropertyTrace, PropertyTraceDto>();
        }
    }
}
