using MongoDB.Bson;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Model;

namespace RealEstate.API.Modules.PropertyTrace.Mapper
{
    public static class PropertyTraceMapper
    {
        // Model → DTO
        public static PropertyTraceDto ToDto(this PropertyTraceModel model) => new()
        {
            IdPropertyTrace = model.Id,
            DateSale = model.DateSale,
            Name = model.Name,
            Value = model.Value,
            Tax = model.Tax,
            IdProperty = model.IdProperty
        };

        // DTO → Model
        public static PropertyTraceModel ToModel(this PropertyTraceDto dto) => new()
        {
            Id = string.IsNullOrEmpty(dto.IdPropertyTrace)
                ? ObjectId.GenerateNewId().ToString() // solo genera si no existe
                : dto.IdPropertyTrace,
            DateSale = dto.DateSale,
            Name = dto.Name,
            Value = dto.Value,
            Tax = dto.Tax,
            IdProperty = dto.IdProperty
        };

        // Listas
        public static List<PropertyTraceDto> ToDtoList(IEnumerable<PropertyTraceModel> models) =>
            models?.Select(ToDto).ToList() ?? new();

        public static List<PropertyTraceModel> ToModelList(IEnumerable<PropertyTraceDto> dtos) =>
            dtos?.Select(ToModel).ToList() ?? new();
    }
}
