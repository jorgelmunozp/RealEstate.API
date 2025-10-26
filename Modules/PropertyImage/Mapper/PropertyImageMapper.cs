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
            IdPropertyImage = model.Id,
            File = model.File,
            Enabled = model.Enabled,
            IdProperty = model.IdProperty
        };

        // 🔹 DTO → Model
        public static PropertyImageModel ToModel(this PropertyImageDto dto) => new()
        {
            Id = string.IsNullOrEmpty(dto.IdPropertyImage)
                ? ObjectId.GenerateNewId().ToString() // genera nuevo Id solo si no existe
                : dto.IdPropertyImage ?? string.Empty,
            File = dto.File ?? string.Empty,
            Enabled = dto.Enabled,
            IdProperty = dto.IdProperty ?? string.Empty
        };

        // 🔹 Lista de modelos → Lista de DTOs
        public static List<PropertyImageDto> ToDtoList(IEnumerable<PropertyImageModel>? models)
        {
            if (models == null || !models.Any())
                return new List<PropertyImageDto>();

            return models.Select(m => m.ToDto()).ToList();
        }

        // 🔹 Lista de DTOs → Lista de modelos
        public static List<PropertyImageModel> ToModelList(IEnumerable<PropertyImageDto>? dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<PropertyImageModel>();

            return dtos.Select(d => d.ToModel()).ToList();
        }
    }
}
