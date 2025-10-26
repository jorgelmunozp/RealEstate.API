using MongoDB.Bson;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Model;

namespace RealEstate.API.Modules.PropertyImage.Mapper
{
    public static class PropertyImageMapper
    {
        // ===========================================================
        // 🔹 Model → DTO
        // ===========================================================
        public static PropertyImageDto ToDto(this PropertyImageModel model)
        {
            if (model == null) return new PropertyImageDto();

            return new PropertyImageDto
            {
                IdPropertyImage = model.Id,
                File = model.File ?? string.Empty,
                Enabled = model.Enabled,
                IdProperty = model.IdProperty ?? string.Empty
            };
        }

        // ===========================================================
        // 🔹 DTO → Model
        // ===========================================================
        public static PropertyImageModel ToModel(this PropertyImageDto dto)
        {
            if (dto == null) return new PropertyImageModel();

            // ⚙️ Si viene sin Id, genera uno nuevo solo en creación (no actualización)
            var id = string.IsNullOrEmpty(dto.IdPropertyImage)
                ? ObjectId.GenerateNewId().ToString()
                : dto.IdPropertyImage!;

            return new PropertyImageModel
            {
                Id = id,
                File = dto.File ?? string.Empty,
                Enabled = dto.Enabled,
                IdProperty = dto.IdProperty ?? string.Empty
            };
        }

        // ===========================================================
        // 🔹 IEnumerable<Model> → List<DTO>
        // ===========================================================
        public static List<PropertyImageDto> ToDtoList(IEnumerable<PropertyImageModel>? models)
        {
            if (models == null) return new List<PropertyImageDto>();
            return models.Select(ToDto).ToList();
        }

        // ===========================================================
        // 🔹 IEnumerable<DTO> → List<Model>
        // ===========================================================
        public static List<PropertyImageModel> ToModelList(IEnumerable<PropertyImageDto>? dtos)
        {
            if (dtos == null) return new List<PropertyImageModel>();
            return dtos.Select(ToModel).ToList();
        }
    }
}
