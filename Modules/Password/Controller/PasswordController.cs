using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.Password.Dto;
using RealEstate.API.Modules.Password.Service;

namespace RealEstate.API.Modules.Password.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordController : ControllerBase
    {
        private readonly PasswordService _service;
        public PasswordController(MongoDB.Driver.IMongoDatabase database, IConfiguration config)
        {
            _service = new PasswordService(database, config);
        }

        // POST: api/password/recover
        [HttpPost("recover")]
        [AllowAnonymous]
        public async Task<IActionResult> Recover([FromBody] PasswordRecoverDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email es requerido" });

            var result = await _service.SendPasswordRecoveryEmail(dto.Email);
            return Ok(result);
        }

        // GET: api/password/reset/{token}
        [HttpGet("reset/{token}")]
        [AllowAnonymous]
        public IActionResult VerifyToken(string token)
        {
            var result = _service.VerifyResetToken(token);
            return Ok(result);
        }

        // PATCH: api/password/update
        [HttpPatch("update")]
        [AllowAnonymous]
        public async Task<IActionResult> Update([FromBody] PasswordUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Token y nueva contraseña son requeridos" });

            try
            {
                var verified = _service.VerifyResetToken(dto.Token) as dynamic;
                string id = verified.id;
                var result = await _service.UpdatePasswordById(id, dto.NewPassword);
                return Ok(result);
            }
            catch
            {
                return Unauthorized(new { message = "Token inválido o expirado" });
            }
        }
    }
}
