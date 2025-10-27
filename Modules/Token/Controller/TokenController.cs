using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Infraestructure.Core.Logs;
using RealEstate.API.Modules.Token.Service;

namespace RealEstate.API.Modules.Token.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public TokenController(JwtService jwtService)
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
                // ✅ El JwtService maneja toda la lógica (validación, usuario, tokens)
                var result = await _jwtService.ProcessRefreshTokenAsync(Request.Headers["Authorization"].ToString());
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceLogResponseWrapper<object>.Fail(
                    $"Error al renovar token: {ex.Message}", statusCode: 500));
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
                    return Unauthorized(ServiceLogResponseWrapper<object>.Fail("Token inválido o expirado", statusCode: 401));

                var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
                return Ok(ServiceLogResponseWrapper<object>.Ok(claims, "Token válido", 200));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ServiceLogResponseWrapper<object>.Fail(
                    $"Error al validar token: {ex.Message}", statusCode: 500));
            }
        }
    }
}
