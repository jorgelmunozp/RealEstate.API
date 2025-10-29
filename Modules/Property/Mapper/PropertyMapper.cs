using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.PropertyImage.Dto;
using MongoDB.Bson;

namespace RealEstate.API.Modules.Property.Mapper
{
    public static class PropertyMapper
    {
              // Model → DTO
              public static PropertyDto ToDto(this PropertyModel model)
        {
            if (model == null) return new PropertyDto();

            return new PropertyDto
            {
                IdProperty = model.Id,
                Name = model.Name,
                Address = model.Address,
                Price = model.Price,
                CodeInternal = model.CodeInternal,
                Year = model.Year,
                IdOwner = model.IdOwner,

                // Ahora el DTO soporta imagen (puede venir del servicio)
                Image = model is IPropertyWithImage imageModel && imageModel.Image != null
                    ? new PropertyImageDto
                    {
                        IdPropertyImage = imageModel.Image.IdPropertyImage,
                        IdProperty = imageModel.Image.IdProperty,
                        File = imageModel.Image.File
                    }
                    : null
            };
        }

        public static List<PropertyDto> ToDtoList(IEnumerable<PropertyModel> models)
            => models?.Select(ToDto).ToList() ?? new List<PropertyDto>();

              // DTO → Model
              public static PropertyModel ToModel(this PropertyDto dto)
        {
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
                // No se incluye la imagen aquí, porque se gestiona en PropertyImageService
            };
        }

        public static List<PropertyModel> ToModelList(IEnumerable<PropertyDto> dtos)
            => dtos?.Select(ToModel).ToList() ?? new List<PropertyModel>();
    }

      // 🔸 Interfaz opcional para compatibilidad con modelos extendidos
      public interface IPropertyWithImage
    {
        PropertyImageDto? Image { get; set; }
    }
}
