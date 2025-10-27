using RealEstate.API.Infraestructure.Core.Logs;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.User.Dto;

namespace RealEstate.API.Modules.Auth.Service
{
    public interface IAuthService
    {
        // ===========================================================
        // LOGIN: Autenticación de usuario
        // ===========================================================
        Task<ServiceLogResponseWrapper<object>> LoginAsync(LoginDto loginDto);

        // ===========================================================
        // REGISTER: Registro de nuevo usuario
        // ===========================================================
        Task<ServiceLogResponseWrapper<object>> RegisterAsync(UserDto userDto);
    }
}
