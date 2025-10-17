using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;

    public AuthController(UserService userService, JwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userService.GetByEmailAsync(request.Email);
        if (existingUser != null) return BadRequest("Usuario ya existe");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = request.Password, // En producción: hashear
            Role = request.Role
        };

        await _userService.CreateAsync(user);

        return Ok(new { message = "Usuario registrado" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null || user.Password != request.Password)
            return Unauthorized(new { message = "Credenciales inválidas" });

        var token = _jwtService.GenerateToken(user);
        return Ok(new { Token = token });
    }
}
