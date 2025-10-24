using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Model;
using MongoDB.Bson;

namespace RealEstate.API.Modules.Owner.Mapper
{
    public static class OwnerMapper
    {
        // De Model → DTO
        public static OwnerDto ToDto(this OwnerModel model) => new()
        {
            Name = model.Name,
            Address = model.Address,
            Photo = model.Photo,
            Birthday = model.Birthday
        };

        // Convierte una lista de modelos en una lista de DTOs
        public static List<OwnerDto> ToDtoList(IEnumerable<OwnerModel> models)
        {
            if (models == null || !models.Any())
                return new List<OwnerDto>();

            return models.Select(m => ToDto(m)).ToList();
        }

        // De DTO → Model
        public static OwnerModel ToModel(this OwnerDto dto) => new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = dto.Name,
            Address = dto.Address,
            Photo = dto.Photo,
            Birthday = dto.Birthday
        };

        // Convierte una lista de DTOs en una lista de modelos
        public static List<OwnerModel> ToModelList(IEnumerable<OwnerDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<OwnerModel>();

            return dtos.Select(d => ToModel(d)).ToList();
        }
    }
}
