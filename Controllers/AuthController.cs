// using Microsoft.AspNetCore.Mvc;
// using BCrypt.Net;

// [ApiController]
// [Route("api/[controller]")]
// public class AuthController : ControllerBase
// {
//     private readonly UserService _userService;
//     private readonly JwtService _jwtService;

//     public AuthController(UserService userService, JwtService jwtService)
//     {
//         _userService = userService;
//         _jwtService = jwtService;
//     }

//     [HttpPost("register")]
//     public async Task<IActionResult> Register([FromBody] RegisterRequest request)
//     {
//         var existingUser = await _userService.GetByEmailAsync(request.Email);
//         if (existingUser != null) return BadRequest("Usuario ya existe");

//         var user = new User
//         {
//             Name = request.Name,
//             Email = request.Email,
//             Password = request.Password, // En producción: hashear
//             Role = request.Role
//         };

//         await _userService.CreateAsync(user);

//         return Ok(new { message = "Usuario registrado" });
//     }

//     [HttpPost("login")]
//     public async Task<IActionResult> Login([FromBody] LoginRequest request)
//     {
//         var user = await _userService.GetByEmailAsync(request.Email);
//         if (user == null || user.Password != request.Password)
//             return Unauthorized(new { message = "Credenciales inválidas" });

//         var token = _jwtService.GenerateToken(user);
//         return Ok(new { Token = token });
//     }
// }



using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

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

    // ============================================
    // 🔹 Registro de usuario (con hash seguro)
    // ============================================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userService.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { message = "El usuario ya existe." });

        // ✅ Hashear la contraseña antes de guardar
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Id = default!,
            Name = request.Name,
            Email = request.Email,
            Password = hashedPassword,
            Role = request.Role
        };

        await _userService.CreateAsync(user);

        return Ok(new { message = "Usuario registrado correctamente." });
    }

    // ============================================
    // 🔹 Login de usuario (comparando hash)
    // ============================================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Credenciales inválidas." });

        // ✅ Comparar la contraseña ingresada con el hash guardado
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!isPasswordValid)
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
}
