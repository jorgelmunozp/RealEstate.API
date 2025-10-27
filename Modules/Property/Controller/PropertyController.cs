using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Service;

namespace RealEstate.API.Modules.Property.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyController : ControllerBase
    {
        private readonly PropertyService _service;

        public PropertyController(PropertyService service)
        {
            _service = service;
        }

        // ===========================================================
        // GET: api/property
        // ===========================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? address,
            [FromQuery] string? idOwner,
            [FromQuery] long? minPrice,
            [FromQuery] long? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 6,
            [FromQuery] bool refresh = false)
        {
            var result = await _service.GetCachedAsync(name, address, idOwner, minPrice, maxPrice, page, limit, refresh);
            return StatusCode(result.StatusCode, result);
        }

        // ===========================================================
        // GET: api/property/{id}
        // ===========================================================
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // ===========================================================
        // POST: api/property
        // ===========================================================
        [HttpPost]
        [Authorize(Roles = "user,editor,admin")]
        public async Task<IActionResult> Create([FromBody] PropertyDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "El cuerpo de la solicitud no puede ser nulo." });

            var result = await _service.CreateAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        // ===========================================================
        // PUT: api/property/{id}
        // ===========================================================
        [HttpPut("{id}")]
        [Authorize(Roles = "editor,admin")]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { success = false, message = "El parámetro 'id' es obligatorio." });

            var result = await _service.UpdateAsync(id, dto);
            return StatusCode(result.StatusCode, result);
        }

        // ===========================================================
        // PATCH: api/property/{id}
        // ===========================================================
        [HttpPatch("{id}")]
        [Authorize(Roles = "editor,admin")]
        public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, object> fields)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { success = false, message = "El parámetro 'id' es obligatorio." });

            if (fields == null || fields.Count == 0)
                return BadRequest(new { success = false, message = "No se enviaron campos válidos para actualizar." });

            var result = await _service.PatchAsync(id, fields);
            return StatusCode(result.StatusCode, result);
        }

        // ===========================================================
        // DELETE: api/property/{id}
        // ===========================================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { success = false, message = "El parámetro 'id' es obligatorio." });

            var result = await _service.DeleteAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}
