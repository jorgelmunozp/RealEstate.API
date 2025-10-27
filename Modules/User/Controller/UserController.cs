using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Service;
using System.Security.Claims;

namespace RealEstate.API.Modules.User.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        // ===========================================================
        // 🔹 Helper: obtener rol actual desde JWT
        // ===========================================================
        private string GetRequesterRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? "user";
            return roleClaim.ToLower();
        }

        // ===========================================================
        // 🔹 GET: api/user
        // ===========================================================
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll([FromQuery] bool refresh = false)
        {
            var result = await _service.GetAllAsync(refresh);
            return Ok(result);
        }

        // ===========================================================
        // 🔹 GET: api/user/{email}
        // ===========================================================
        [HttpGet("{email}")]
        public async Task<IActionResult> GetByEmail(string email, [FromQuery] bool refresh = false)
        {
            var user = await _service.GetByEmailAsync(email, refresh);
            return user is not null
                ? Ok(user)
                : NotFound(new { Message = "Usuario no encontrado" });
        }

        // ===========================================================
        // 🔹 POST: api/user
        // ===========================================================
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] UserDto user)
        {
            var result = await _service.CreateUserAsync(user);
            return result.Success
                ? CreatedAtAction(nameof(GetByEmail), new { email = result.Data!.Email }, result.Data)
                : StatusCode(result.StatusCode, new { Message = result.Message, Errors = result.Errors });
        }

        // ===========================================================
        // 🔹 PUT: api/user/{email}
        // ===========================================================
        [HttpPut("{email}")]
        [Authorize(Roles = "user,editor,admin")]
        public async Task<IActionResult> Update(string email, [FromBody] UserDto user)
        {
            var requesterRole = GetRequesterRole();
            var requesterEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            // 🔒 Solo admin o el propio usuario puede editar
            if (requesterRole != "admin" && !string.Equals(email, requesterEmail, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var result = await _service.UpdateUserAsync(email, user, requesterRole);

            return result.StatusCode switch
            {
                404 => NotFound(new { Message = result.Message }),
                403 => Forbid(),
                400 => BadRequest(new { Errors = result.Errors }),
                _ => Ok(result.Data)
            };
        }

        // ===========================================================
        // 🔹 PATCH: api/user/{email}
        // ===========================================================
        [HttpPatch("{email}")]
        [Authorize(Roles = "user,editor,admin")]
        public async Task<IActionResult> Patch(string email, [FromBody] Dictionary<string, object> fields)
        {
            var requesterRole = GetRequesterRole();
            var requesterEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            // 🔒 Solo admin o el propio usuario puede editar
            if (requesterRole != "admin" && !string.Equals(email, requesterEmail, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var result = await _service.PatchUserAsync(email, fields, requesterRole);

            return result.StatusCode switch
            {
                404 => NotFound(new { Message = result.Message }),
                403 => Forbid(),
                400 => BadRequest(new { Errors = result.Errors }),
                _ => Ok(result.Data)
            };
        }

        // ===========================================================
        // 🔹 DELETE: api/user/{email}
        // ===========================================================
        [HttpDelete("{email}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string email)
        {
            var result = await _service.DeleteUserAsync(email);
            return result.StatusCode switch
            {
                404 => NotFound(new { Message = result.Message }),
                _ => NoContent()
            };
        }
    }
}
