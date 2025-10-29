using MongoDB.Bson;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Model;

namespace RealEstate.API.Modules.User.Mapper
{
    public static class UserMapper
    {
              // Model → DTO
              public static UserDto ToDto(this UserModel model)
        {
            if (model == null) return new UserDto();

            return new UserDto
            {
                Id = model.Id,
                Name = model.Name,
                Email = model.Email,
                // ⚠️ No exponer contraseñas en respuestas API
                Password = string.Empty,
                Role = model.Role
            };
        }

        public static List<UserDto> ToDtoList(IEnumerable<UserModel>? models) =>
            models?.Select(m => m.ToDto()).ToList() ?? new List<UserDto>();

              // DTO → Model
              public static UserModel ToModel(this UserDto dto)
        {
            if (dto == null) return new UserModel();

            return new UserModel
            {
                Id = string.IsNullOrWhiteSpace(dto.Id)
                    ? ObjectId.GenerateNewId().ToString()
                    : dto.Id,
                Name = dto.Name,
                Email = dto.Email,
                // ⚙️ Si el servicio ya encripta, aquí se pasa plano
                Password = dto.Password ?? string.Empty,
                Role = dto.Role ?? "user",
                CreatedAt = DateTime.UtcNow
            };
        }

        public static List<UserModel> ToModelList(IEnumerable<UserDto>? dtos) =>
            dtos?.Select(d => d.ToModel()).ToList() ?? new List<UserModel>();
    }
}
