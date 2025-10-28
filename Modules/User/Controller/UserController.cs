using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Interface;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RealEstate.API.Modules.User.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
        {
            _service = service;
        }

        // ===========================================================
        // Helper: obtener rol y correo actual desde JWT
        // ===========================================================
        private (string role, string email) GetRequesterDetails()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "user";
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            return (roleClaim.ToLower(), emailClaim);
        }

        // ===========================================================
        // GET: api/user
        // ===========================================================
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll([FromQuery] bool refresh = false)
        {
            var result = await _service.GetAllAsync(refresh);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, new { Message = result.Message, Errors = result.Errors });
        }

        // ===========================================================
        // GET: api/user/{email}
        // ===========================================================
        [HttpGet("{email}")]
        public async Task<IActionResult> GetByEmail(string email, [FromQuery] bool refresh = false)
        {
            var result = await _service.GetByEmailAsync(email, refresh);
            return result.Success ? Ok(result.Data) : StatusCode(result.StatusCode, new { Message = result.Message });
        }

        // ===========================================================
        // POST: api/user
        // ===========================================================
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] UserDto user)
        {
            if (user == null)
                return BadRequest(new { Success = false, Message = "El cuerpo de la solicitud no puede ser nulo." });

            var result = await _service.CreateUserAsync(user);
            return result.Success ? CreatedAtAction(nameof(GetByEmail), new { email = result.Data.Email }, result.Data) : StatusCode(result.StatusCode, new { Message = result.Message, Errors = result.Errors });
        }

        // ===========================================================
        // PUT: api/user/{email}
        // ===========================================================
        [HttpPut("{email}")]
        [Authorize(Roles = "user,editor,admin")]
        public async Task<IActionResult> Update(string email, [FromBody] UserDto user)
        {
            var (requesterRole, requesterEmail) = GetRequesterDetails();

            if (requesterRole != "admin" && !string.Equals(email, requesterEmail, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var result = await _service.UpdateUserAsync(email, user, requesterRole);
            return result.StatusCode switch
            {
                404 => NotFound(new { Message = result.Message }),
                403 => Forbid(),
                400 => BadRequest(new { Errors = result.Errors }),
                _ => Ok(result.Data),
            };
        }

        // ===========================================================
        // PATCH: api/user/{email}
        // ===========================================================
        [HttpPatch("{email}")]
        [Authorize(Roles = "user,editor,admin")]
        public async Task<IActionResult> Patch(string email, [FromBody] Dictionary<string, object> fields)
        {
            var (requesterRole, requesterEmail) = GetRequesterDetails();

            if (requesterRole != "admin" && !string.Equals(email, requesterEmail, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var result = await _service.PatchUserAsync(email, fields, requesterRole);
            return result.StatusCode switch
            {
                404 => NotFound(new { Message = result.Message }),
                403 => Forbid(),
                400 => BadRequest(new { Errors = result.Errors }),
                _ => Ok(result.Data),
            };
        }

        // ===========================================================
        // DELETE: api/user/{email}
        // ===========================================================
        [HttpDelete("{email}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string email)
        {
            var result = await _service.DeleteUserAsync(email);
            return result.StatusCode switch
            {
                404 => NotFound(new { Message = result.Message }),
                _ => NoContent(),
            };
        }
    }
}
