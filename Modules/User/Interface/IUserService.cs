using RealEstate.API.Infraestructure.Core.Services;  // Para ServiceResultWrapper<T>
using RealEstate.API.Modules.User.Dto;               // Para UserDto
using FluentAssertions;

namespace RealEstate.API.Modules.User.Interface
{
    public interface IUserService
    {
        Task<ServiceResultWrapper<List<UserDto>>> GetAllAsync(bool refresh = false);
        Task<ServiceResultWrapper<UserDto?>> GetByEmailAsync(string email, bool refresh = false);
        Task<ServiceResultWrapper<UserDto?>> GetByIdAsync(string id, bool refresh = false);
        Task<ServiceResultWrapper<UserDto>> CreateUserAsync(UserDto user);
        Task<ServiceResultWrapper<UserDto>> UpdateUserAsync(string email, UserDto user, string requesterRole);
        Task<ServiceResultWrapper<UserDto>> PatchUserAsync(string email, Dictionary<string, object> fields, string requesterRole);
        Task<ServiceResultWrapper<bool>> DeleteUserAsync(string email);
    }
}
