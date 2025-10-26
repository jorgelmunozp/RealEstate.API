using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
            [FromQuery] int limit = 10,
            [FromQuery] bool refresh = false)
        {
            var result = await _service.GetAllAsync(idProperty, enabled, page, limit, refresh);
            return Ok(result);
        }

        // 🔹 GET: api/propertyimage/{idPropertyImage}
        [HttpGet("{idPropertyImage}")]
        public async Task<IActionResult> GetById(string idPropertyImage)
        {
            var image = await _service.GetByIdAsync(idPropertyImage);
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
        [Authorize]
        public async Task<IActionResult> Create([FromBody] PropertyImageDto image)
        {
            var id = await _service.CreateAsync(image);
            return CreatedAtAction(nameof(GetById), new { idPropertyImage = id }, new { IdPropertyImage = id });
        }

        // 🔹 PUT: api/propertyimage/{idPropertyImage}
        [HttpPut("{idPropertyImage}")]
        [Authorize(Roles = "editor,admin")]
        public async Task<IActionResult> Update(string idPropertyImage, [FromBody] PropertyImageDto image)
        {
            var result = await _service.UpdateAsync(idPropertyImage, image);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "IdPropertyImage"))
                    return NotFound(new { Message = "Imagen no encontrada" });

                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updated = await _service.GetByIdAsync(idPropertyImage);
            return Ok(updated);
        }

        // 🔹 PATCH: api/propertyimage/{idPropertyImage}
        [HttpPatch("{idPropertyImage}")]
        [Authorize(Roles = "editor,admin")]
        public async Task<IActionResult> Patch(string idPropertyImage, [FromBody] PropertyImageDto image)
        {
            var result = await _service.UpdatePartialAsync(idPropertyImage, image);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "IdPropertyImage"))
                    return NotFound(new { Message = "Imagen no encontrada" });

                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updated = await _service.GetByIdAsync(idPropertyImage);
            return Ok(updated);
        }

        // 🔹 DELETE: api/propertyimage/{idPropertyImage}
        [HttpDelete("{idPropertyImage}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string idPropertyImage)
        {
            bool deleted = await _service.DeleteAsync(idPropertyImage);
            if (!deleted)
                return NotFound(new { Message = "Imagen no encontrada" });

            return NoContent();
        }
    }
}
