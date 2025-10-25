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
        [Authorize]
        public async Task<IActionResult> Update(string email, [FromBody] UserDto user)
        {
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
