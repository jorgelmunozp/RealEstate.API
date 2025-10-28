using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.Token.Interface;

namespace RealEstate.API.Modules.Token.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly IJwtService _jwtService;

        public TokenController(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        // ===========================================================
        // POST: /api/token/refresh
        // ===========================================================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                var result = await _jwtService.ProcessRefreshTokenAsync(authHeader);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResultWrapper<object>.Error(
                    new Exception($"Error al renovar token: {ex.Message}")));
            }
        }

        // ===========================================================
        // GET: /api/token/validate?token=...
        // ===========================================================
        [HttpGet("validate")]
        [AllowAnonymous]
        public IActionResult Validate([FromQuery] string token)
        {
            try
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal == null)
                    return Unauthorized(ServiceResultWrapper<object>.Fail(
                        "Token inválido o expirado", 401));

                var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
                return Ok(ServiceResultWrapper<object>.Ok(claims, "Token válido"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceResultWrapper<object>.Error(
                    new Exception($"Error al validar token: {ex.Message}")));
            }
        }
    }
}
