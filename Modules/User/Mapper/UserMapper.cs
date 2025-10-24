using MongoDB.Bson;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Model;

namespace RealEstate.API.Modules.User.Mapper
{
    public static class UserMapper
    {
        // Model → DTO
        public static UserDto ToDto(this UserModel model) => new()
        {
            Name = model.Name,
            Email = model.Email,
            Password = model.Password,
            Role = model.Role
        };

        // Convierte una lista de modelos en una lista de DTOs
        public static List<UserDto> ToDtoList(IEnumerable<UserModel> models)
        {
            if (models == null || !models.Any())
                return new List<UserDto>();

            return models.Select(m => ToDto(m)).ToList();
        }

        // DTO → Model
        public static UserModel ToModel(this UserDto dto) => new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = dto.Name,
            Email = dto.Email,
            Password = dto.Password,
            Role = dto.Role
        };

        // Convierte una lista de DTOs en una lista de modelos
        public static List<UserModel> ToModelList(IEnumerable<UserDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return new List<UserModel>();

            return dtos.Select(d => ToModel(d)).ToList();
        }
    }
}
