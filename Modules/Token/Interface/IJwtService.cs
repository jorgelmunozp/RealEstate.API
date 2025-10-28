using System.Security.Claims;
using System.Threading.Tasks;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.User.Model;

namespace RealEstate.API.Modules.Token.Interface
{
    public interface IJwtService
    {
        string GenerateToken(UserModel user);
        string GenerateRefreshToken(UserModel user);
        (string AccessToken, string RefreshToken) GenerateTokens(UserModel user);
        ClaimsPrincipal? ValidateToken(string token);
        Task<ServiceResultWrapper<object>> ProcessRefreshTokenAsync(string authHeader);
        (string AccessToken, string RefreshToken) RefreshAccessToken(string refreshToken, UserModel user);
    }
}
