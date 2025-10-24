using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using MongoDB.Bson;

namespace RealEstate.API.Modules.Property.Mapper
{
    public static class PropertyMapper
    {
        // De Model → DTO
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

        // Convierte una lista de modelos en una lista de DTOs
        public static List<PropertyDto> ToDtoList(IEnumerable<PropertyModel> models)
        {
            if (models == null || !models.Any())
                return new List<PropertyDto>();

            return models.Select(m => ToDto(m)).ToList();
        }

        // De DTO → Model
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

        // Convierte una lista de DTOs en una lista de modelos
        public static List<PropertyModel> ToModelList(IEnumerable<PropertyDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<PropertyModel>();

            return dtos.Select(d => ToModel(d)).ToList();
        }
    }
}
