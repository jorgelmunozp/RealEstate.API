using FluentValidation.Results;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.User.Dto;

namespace RealEstate.API.Modules.Auth.Service
{
    public interface IAuthService
    {
        Task<string> LoginAsync(LoginDto loginDto);
        Task<ValidationResult> RegisterAsync(UserDto userDto);
    }
}
