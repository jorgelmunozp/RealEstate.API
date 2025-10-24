using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Model;
using MongoDB.Bson;

namespace RealEstate.API.Modules.PropertyImage.Mapper
{
    public static class PropertyImageMapper
    {
        // De Model → DTO
        public static PropertyImageDto ToDto(this PropertyImageModel model) => new()
        {
            File = model.File,
            Enabled = model.Enabled,
            IdProperty = model.IdProperty
        };

        // Convierte una lista de modelos en una lista de DTOs
        public static List<PropertyImageDto> ToDtoList(IEnumerable<PropertyImageModel> models)
        {
            if (models == null || !models.Any())
                return new List<PropertyImageDto>();

            return models.Select(m => ToDto(m)).ToList();
        }

        // De DTO → Model
        public static PropertyImageModel ToModel(this PropertyImageDto dto) => new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            File = dto.File,
            Enabled = dto.Enabled,
            IdProperty = dto.IdProperty
        };

        // Convierte una lista de DTOs en una lista de modelos
        public static List<PropertyImageModel> ToModelList(IEnumerable<PropertyImageDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<PropertyImageModel>();

            return dtos.Select(d => ToModel(d)).ToList();
        }
    }
}
