using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using MongoDB.Bson;

namespace RealEstate.API.Modules.Property.Mapper
{
    public static class PropertyMapper
    {
        public static PropertyDto ToDto(this PropertyModel model) => new()
        {
            IdProperty = model.Id,
            Name = model.Name,
            Address = model.Address,
            Price = model.Price,
            CodeInternal = model.CodeInternal,
            Year = model.Year,
            IdOwner = model.IdOwner
        };

        public static List<PropertyDto> ToDtoList(IEnumerable<PropertyModel> models)
            => models?.Select(ToDto).ToList() ?? new List<PropertyDto>();

        public static PropertyModel ToModel(this PropertyDto dto) => new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = dto.Name,
            Address = dto.Address,
            Price = dto.Price,
            CodeInternal = dto.CodeInternal,
            Year = dto.Year,
            IdOwner = dto.IdOwner
        };

        public static List<PropertyModel> ToModelList(IEnumerable<PropertyDto> dtos)
            => dtos?.Select(ToModel).ToList() ?? new List<PropertyModel>();
    }
}
