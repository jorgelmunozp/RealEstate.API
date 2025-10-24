using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using MongoDB.Bson;

namespace RealEstate.API.Modules.Property.Mapper
{
    public static class PropertyMapper
    {
        // ===========================================================
        // 🔹 Model → DTO
        // ===========================================================
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

        // ===========================================================
        // 🔹 DTO → Model
        // ===========================================================
        public static PropertyModel ToModel(this PropertyDto dto)
        {
            // Si el DTO trae IdProperty (por PATCH o UPDATE), respétalo
            var id = !string.IsNullOrEmpty(dto.IdProperty)
                ? dto.IdProperty
                : ObjectId.GenerateNewId().ToString();

            return new PropertyModel
            {
                Id = id,
                Name = dto.Name,
                Address = dto.Address,
                Price = dto.Price,
                CodeInternal = dto.CodeInternal,
                Year = dto.Year,
                IdOwner = dto.IdOwner
            };
        }

        public static List<PropertyModel> ToModelList(IEnumerable<PropertyDto> dtos)
            => dtos?.Select(ToModel).ToList() ?? new List<PropertyModel>();
    }
}
