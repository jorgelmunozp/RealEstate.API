using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.Auth.Service;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.User.Dto;
using FluentValidation;
using FluentValidation.Results;

namespace RealEstate.API.Modules.Auth.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IValidator<LoginDto> _validator;
        private readonly IValidator<UserDto> _userValidator;

        public AuthController(AuthService authService, IValidator<LoginDto> validator, IValidator<UserDto> userValidator)
        {
            _authService = authService;
            _validator = validator;
            _userValidator = userValidator;
        }

        //********* Login Endpoint *********/
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Valida DTO
            ValidationResult validationResult = await _validator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            try
            {
                // 2️⃣ Ejecutar login
                string token = await _authService.LoginAsync(loginDto);

                // 3️⃣ Devolver token
                return Ok(new { Token = token });
            }
            catch (InvalidOperationException ex)
            {
                // Usuario o contraseña incorrectos
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Error genérico
                return StatusCode(500, new { message = ex.Message });
            }
        }

        //********* Register Endpoint *********/
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            ValidationResult validationResult = await _userValidator.ValidateAsync(userDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            try
            {
                ValidationResult result = await _authService.RegisterAsync(userDto);
                if (!result.IsValid)
                {
                    return BadRequest(result.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
                }

                return Ok(new { message = "Usuario registrado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
