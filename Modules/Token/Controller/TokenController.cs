using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.Token.Service;
using RealEstate.API.Modules.Token.Dto;
using RealEstate.API.Modules.User.Model;

namespace RealEstate.API.Modules.Token.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public TokenController(IConfiguration config)
        {
            _jwtService = new JwtService(config);
        }

        // =========================================================
        // POST: api/token/generate
        // =========================================================
        [HttpPost("generate")]
        [AllowAnonymous]
        public IActionResult Generate([FromBody] UserModel user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return BadRequest(new { message = "Datos de usuario requeridos" });

            var tokens = _jwtService.GenerateTokens(user);
            return Ok(new
            {
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken,
                message = "Tokens generados correctamente"
            });
        }

        // =========================================================
        // POST: api/token/refresh
        // =========================================================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody] RefreshTokenRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(new { message = "El refresh token es requerido" });

            var user = new UserModel
            {
                Id = dto.UserId ?? "unknown",
                Name = dto.UserName ?? "Unknown",
                Email = dto.UserEmail ?? "unknown@email.com",
                Role = dto.UserRole ?? "user"
            };

            try
            {
                var result = _jwtService.RefreshAccessToken(dto.RefreshToken, user);
                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    message = "Tokens renovados correctamente"
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // =========================================================
        // POST: api/token/validate
        // =========================================================
        [HttpPost("validate")]
        [AllowAnonymous]
        public IActionResult Validate([FromBody] ValidateTokenRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest(new { message = "Token requerido" });

            var principal = _jwtService.ValidateToken(dto.Token);
            if (principal == null)
                return Unauthorized(new { message = "Token inválido o expirado" });

            var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
            return Ok(new { message = "Token válido", claims });
        }
    }
}
