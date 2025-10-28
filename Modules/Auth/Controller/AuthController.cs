using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.Auth.Service;
using RealEstate.API.Modules.Auth.Interface;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.User.Dto;

namespace RealEstate.API.Modules.Auth.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ===========================================================
        // POST: /api/auth/login
        // ===========================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null)
                return BadRequest(new { Message = "El cuerpo de la solicitud no puede estar vacío." });

            var result = await _authService.LoginAsync(dto);

            return result.StatusCode switch
            {
                400 => BadRequest(new { Message = result.Message, Errors = result.Errors }),
                401 => Unauthorized(new { Message = result.Message }),
                _ => StatusCode(result.StatusCode, new
                {
                    Message = result.Message,
                    Data = result.Data
                })
            };
        }

        // ===========================================================
        // POST: /api/auth/register
        // ===========================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto dto)
        {
            if (dto == null)
                return BadRequest(new { Message = "El cuerpo de la solicitud no puede estar vacío." });

            var result = await _authService.RegisterAsync(dto);

            return result.StatusCode switch
            {
                400 => BadRequest(new { Message = result.Message, Errors = result.Errors }),
                401 => Unauthorized(new { Message = result.Message }),
                201 => Created("", new { Message = result.Message, Data = result.Data }),
                _ => StatusCode(result.StatusCode, new { Message = result.Message, Data = result.Data })
            };
        }
    }
}
