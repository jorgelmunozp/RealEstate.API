using MongoDB.Bson;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Model;

namespace RealEstate.API.Modules.PropertyImage.Mapper
{
    public static class PropertyImageMapper
    {
        // 🔹 Model → DTO
        public static PropertyImageDto ToDto(this PropertyImageModel model) => new()
        {
            Id = model.Id,
            File = model.File,
            Enabled = model.Enabled,
            IdProperty = model.IdProperty
        };

        // 🔹 DTO → Model
        public static PropertyImageModel ToModel(this PropertyImageDto dto) => new()
        {
            Id = string.IsNullOrEmpty(dto.Id)
                ? ObjectId.GenerateNewId().ToString() // solo genera nuevo Id si no existe
                : dto.Id,
            File = dto.File ?? string.Empty,
            Enabled = dto.Enabled,
            IdProperty = dto.IdProperty ?? string.Empty
        };

        // 🔹 Convierte lista de modelos → DTOs
        public static List<PropertyImageDto> ToDtoList(IEnumerable<PropertyImageModel> models)
        {
            if (models == null || !models.Any())
                return new List<PropertyImageDto>();

            return models.Select(m => ToDto(m)).ToList();
        }

        // 🔹 Convierte lista de DTOs → Modelos
        public static List<PropertyImageModel> ToModelList(IEnumerable<PropertyImageDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<PropertyImageModel>();

            return dtos.Select(d => ToModel(d)).ToList();
        }
    }
}
