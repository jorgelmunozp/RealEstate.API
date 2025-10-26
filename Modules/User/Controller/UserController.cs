using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Service;

namespace RealEstate.API.Modules.User.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        // GET: api/user
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] bool refresh = false) =>
            Ok(await _service.GetAllAsync(refresh));

        // GET: api/user/{email}
        [HttpGet("{email}")]
        [Authorize]
        public async Task<IActionResult> GetByEmail(string email, [FromQuery] bool refresh = false)
        {
            var user = await _service.GetByEmailAsync(email, refresh);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado" });
            return Ok(user);
        }

        // POST: api/user
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] UserDto user)
        {
            var result = await _service.CreateAsync(user);
            if (!result.IsValid) return BadRequest(result.Errors.Select(e => e.ErrorMessage));

            return CreatedAtAction(nameof(GetByEmail), new { email = user.Email }, user);
        }

        // PUT: api/user/{email}
        [HttpPut("{email}")]
        [Authorize(Roles = "editor,admin")]
        public async Task<IActionResult> Update(string email, [FromBody] UserDto user)
        {
            // Restringe cambio de Role a admin
            if (!User.IsInRole("admin"))
            {
                var existing = await _service.GetByEmailAsync(email);
                if (existing == null)
                    return NotFound(new { Message = "Usuario no encontrado" });

                if (!string.Equals(existing.Role, user.Role, StringComparison.OrdinalIgnoreCase))
                    return Forbid();
            }

            var result = await _service.UpdateAsync(email, user);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "Email"))
                    return NotFound(new { Message = "Usuario no encontrado" });

                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updatedUser = await _service.GetByEmailAsync(email);
            return Ok(updatedUser);
        }

        // PATCH: api/user/{email}
        [HttpPatch("{email}")]
        [Authorize(Roles = "editor,admin")]
        public async Task<IActionResult> Patch(string email, [FromBody] Dictionary<string, object> fields)
        {
            if (fields == null || fields.Count == 0)
                return BadRequest(new { Message = "No se enviaron campos para actualizar" });

            // Restringe cambio de Role a admin
            if (!User.IsInRole("admin") && fields.Keys.Any(k => string.Equals(k, "role", StringComparison.OrdinalIgnoreCase)))
                return Forbid();

            var updated = await _service.PatchAsync(email, fields);
            if (updated == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            return Ok(updated);
        }

        // DELETE: api/user/{email}
        [HttpDelete("{email}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string email)
        {
            bool deleted = await _service.DeleteAsync(email);
            if (!deleted) return NotFound(new { Message = "Usuario no encontrado" });

            return NoContent();
        }
    }
}
