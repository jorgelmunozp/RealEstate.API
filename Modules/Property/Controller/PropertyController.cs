using Microsoft.AspNetCore.JsonPatch;
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
        // GET: api/property  (con filtros, paginación y caché)
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
            var property = await _service.GetByIdAsync(id);
            if (property == null) return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(property);
        }

        // ===========================================================
        // POST: api/property
        // ===========================================================
        [HttpPost]
        // [Authorize]
        public async Task<IActionResult> Create([FromBody] PropertyDto property)
        {
            var id = await _service.CreateAsync(property);
            return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
        }

        // ===========================================================
        // PUT: api/property/{id}
        // ===========================================================
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyDto property)
        {
            var result = await _service.UpdateAsync(id, property);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "Id"))
                    return NotFound(new { message = "Propiedad no encontrada" });

                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updated = await _service.GetByIdAsync(id);
            return Ok(updated);
        }

        // ===========================================================
        // PATCH: api/property/{id}
        // ===========================================================
        [HttpPatch("{id}")]
        // [Authorize]
        public async Task<IActionResult> Patch(string id, [FromBody] PropertyDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "El cuerpo de la solicitud no puede estar vacío" });

            // 🔹 Verificar si existe la propiedad
            var existingDto = await _service.GetByIdAsync(id);
            if (existingDto == null)
                return NotFound(new { message = "Propiedad no encontrada" });

            // 🔹 Actualizar la entidad
            var updateResult = await _service.UpdateAsync(id, dto);

            // Si el servicio devuelve un objeto de validación
            if (!updateResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Errores de validación",
                    errors = updateResult.Errors.Select(e => e.ErrorMessage)
                });
            }

            // 🔹 Obtener la versión actualizada
            var updated = await _service.GetByIdAsync(id);
            return Ok(updated);
        }



        // ===========================================================
        // DELETE: api/property/{id}
        // ===========================================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(new { message = "Propiedad eliminada correctamente" });
        }
    }
}
