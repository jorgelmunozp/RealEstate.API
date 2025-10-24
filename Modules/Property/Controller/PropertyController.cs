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
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? address,
            [FromQuery] string? idOwner,
            [FromQuery] long? minPrice,
            [FromQuery] long? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 6)
        {
            var result = await _service.GetCachedAsync(name, address, idOwner, minPrice, maxPrice, page, limit);
            return Ok(result);
        }

        // ===========================================================
        // GET: api/property/{id}
        // ===========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null
                ? NotFound(new { message = "Propiedad no encontrada" })
                : Ok(result);
        }

        // ===========================================================
        // POST: api/property
        // ===========================================================
        [HttpPost]
        // [Authorize] // (opcional)
        public async Task<IActionResult> Create([FromBody] PropertyDto property)
        {
            if (property == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío" });

            var result = await _service.CreateAsync(property);
            return !result.Success
                ? StatusCode(result.StatusCode, new { result.Message, result.Errors })
                : CreatedAtAction(nameof(GetById), new { id = result.Data?.IdProperty }, result.Data);
        }

        // ===========================================================
        // PUT: api/property/{id}
        // ===========================================================
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyDto property)
        {
            if (property == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío" });

            var result = await _service.UpdateAsync(id, property);
            return !result.Success
                ? StatusCode(result.StatusCode, new { result.Message, result.Errors })
                : Ok(result.Data);
        }

        // ===========================================================
        // PATCH: api/property/{id}
        // ===========================================================
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, object> fields)
        {
            if (fields == null || fields.Count == 0)
                return BadRequest(new { message = "No se enviaron campos a actualizar" });

            var result = await _service.PatchAsync(id, fields);
            return !result.Success
                ? StatusCode(result.StatusCode, new { result.Message, result.Errors })
                : Ok(result.Data);
        }

        // ===========================================================
        // DELETE: api/property/{id}
        // ===========================================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            return !result.Success
                ? StatusCode(result.StatusCode, new { result.Message, result.Errors })
                : Ok(new { result.Message });
        }
    }
}
