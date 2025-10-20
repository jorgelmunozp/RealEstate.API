using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Models;
using RealEstate.API.Dtos;
using RealEstate.API.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;

    public AuthController(AuthService authService, JwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    // ============================================
    // 🔹 Registro de usuario
    // ============================================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var user = new User
            {
                Id = default!,
                Name = request.Name,
                Email = request.Email,
                Password = request.Password, // el hash lo hace AuthService
                Role = request.Role
            };

            var newUser = await _authService.RegisterAsync(user);

            return Ok(new
            {
                message = "Usuario registrado correctamente.",
                user = new
                {
                    id = newUser.Id,
                    name = newUser.Name,
                    email = newUser.Email,
                    role = newUser.Role
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================
    // 🔹 Login de usuario
    // ============================================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _authService.LoginAsync(request.Email, request.Password);
        if (user == null)
            return Unauthorized(new { message = "Credenciales inválidas." });

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            message = "Login exitoso.",
            token,
            user = new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role
            }
        });
    }

    // ============================================
    // 🔹 Actualizar contraseña
    // ============================================
    [HttpPatch("password/update")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var success = await _authService.UpdatePasswordAsync(dto.UserId, dto.NewPassword);
        if (!success)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(new { message = "Contraseña actualizada correctamente." });
    }
}
