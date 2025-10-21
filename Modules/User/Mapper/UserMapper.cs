using MongoDB.Bson;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Model;

namespace RealEstate.API.Modules.User.Mapper
{
    public static class UserMapper
    {
        // Model -> DTO
        public static UserDto ToDto(this UserModel model) => new()
        {
            Name = model.Name,
            Email = model.Email,
            Password = model.Password,
            Role = model.Role
        };

        // DTO -> Model
        public static UserModel ToModel(this UserDto dto) => new UserModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = dto.Name,
            Email = dto.Email,
            Password = dto.Password,
            Role = dto.Role
        };
    }
}
