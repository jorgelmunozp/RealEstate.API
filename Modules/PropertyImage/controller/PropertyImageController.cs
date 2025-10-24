using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Service;

namespace RealEstate.API.Modules.PropertyImage.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyImageController : ControllerBase
    {
        private readonly PropertyImageService _service;

        public PropertyImageController(PropertyImageService service)
        {
            _service = service;
        }

        // 🔹 GET: api/propertyimage?idProperty&enabled&page&limit
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? idProperty,
            [FromQuery] bool? enabled,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            var result = await _service.GetAllAsync(idProperty, enabled, page, limit);
            return Ok(result);
        }

        // 🔹 GET: api/propertyimage/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null)
                return NotFound(new { Message = "Imagen no encontrada" });

            return Ok(image);
        }

        // 🔹 GET: api/propertyimage/property/{propertyId}
        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetByPropertyId(string propertyId)
        {
            var image = await _service.GetByPropertyIdAsync(propertyId);
            if (image == null)
                return NotFound(new { Message = "No se encontró imagen asociada a esta propiedad" });

            return Ok(image);
        }

        // 🔹 POST: api/propertyimage
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyImageDto image)
        {
            var id = await _service.CreateAsync(image);
            return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
        }

        // 🔹 PUT: api/propertyimage/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyImageDto image)
        {
            var result = await _service.UpdateAsync(id, image);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "Id"))
                    return NotFound(new { Message = "Imagen no encontrada" });

                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updated = await _service.GetByIdAsync(id);
            return Ok(updated);
        }

        // 🔹 PATCH: api/propertyimage/{id}
        // Actualiza parcialmente solo los campos enviados
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] PropertyImageDto image)
        {
            var result = await _service.UpdatePartialAsync(id, image);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "Id"))
                    return NotFound(new { Message = "Imagen no encontrada" });

                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updated = await _service.GetByIdAsync(id);
            return Ok(updated);
        }

        // 🔹 DELETE: api/propertyimage/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            bool deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { Message = "Imagen no encontrada" });

            return NoContent();
        }
    }
}
