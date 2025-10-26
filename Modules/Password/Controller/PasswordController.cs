using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.Password.Dto;
using RealEstate.API.Modules.Password.Service;
using RealEstate.API.Modules.Token.Service;

namespace RealEstate.API.Modules.Password.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordController : ControllerBase
    {
        private readonly PasswordService _passwordService;

        // ✅ Constructor con inyección de dependencias
        public PasswordController(PasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        // =========================================================
        // POST: api/password/recover
        // 🔹 Envía un correo con el enlace de recuperación
        // =========================================================
        [HttpPost("recover")]
        [AllowAnonymous]
        public async Task<IActionResult> Recover([FromBody] PasswordRecoverDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "El correo electrónico es requerido." });

            try
            {
                var result = await _passwordService.SendPasswordRecoveryEmail(dto.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al enviar el correo: {ex.Message}" });
            }
        }

        // =========================================================
        // GET: api/password/reset/{token}
        // 🔹 Verifica si el token de recuperación es válido
        // =========================================================
        [HttpGet("reset/{token}")]
        [AllowAnonymous]
        public IActionResult VerifyToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "El token es requerido." });

            try
            {
                var result = _passwordService.VerifyResetToken(token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // =========================================================
        // PATCH: api/password/update
        // 🔹 Cambia la contraseña del usuario tras verificar el token
        // =========================================================
        [HttpPatch("update")]
        [AllowAnonymous]
        public async Task<IActionResult> Update([FromBody] PasswordUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "El token y la nueva contraseña son requeridos." });

            try
            {
                // ✅ Verificamos el token usando PasswordService (internamente usa JwtService)
                var verified = _passwordService.VerifyResetToken(dto.Token) as dynamic;
                string id = verified.id;

                var result = await _passwordService.UpdatePasswordById(id, dto.NewPassword);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al actualizar la contraseña: {ex.Message}" });
            }
        }
    }
}
