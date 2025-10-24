using MongoDB.Bson;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Model;

namespace RealEstate.API.Modules.PropertyTrace.Mapper
{
    public static class PropertyTraceMapper
    {
        // De Model → DTO
        public static PropertyTraceDto ToDto(this PropertyTraceModel model) => new()
        {
            DateSale = model.DateSale,
            Name = model.Name,
            Value = model.Value,
            Tax = model.Tax,
            IdProperty = model.IdProperty
        };

        // Convierte una lista de modelos en una lista de DTOs
        public static List<PropertyTraceDto> ToDtoList(IEnumerable<PropertyTraceModel> models)
        {
            if (models == null || !models.Any())
                return new List<PropertyTraceDto>();

            return models.Select(m => ToDto(m)).ToList();
        }

        // De DTO → Model
        public static PropertyTraceModel ToModel(this PropertyTraceDto dto) => new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            DateSale = dto.DateSale,
            Name = dto.Name,
            Value = dto.Value,
            Tax = dto.Tax,
            IdProperty = dto.IdProperty
        };

        // Convierte una lista de DTOs en una lista de modelos
        public static List<PropertyTraceModel> ToModelList(IEnumerable<PropertyTraceDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<PropertyTraceModel>();

            return dtos.Select(d => ToModel(d)).ToList();
        }
    }
}
