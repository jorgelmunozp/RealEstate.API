using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Services;
using RealEstate.API.Models;

namespace RealEstate.API.Controllers
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

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? address,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            var result = await _service.GetCachedAsync(name, address, minPrice, maxPrice, page, limit);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var property = await _service.GetByIdAsync(id);
            return property != null ? Ok(property) : NotFound(new { message = "Propiedad no encontrada" });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Property property)
        {
            if (property == null)
                return BadRequest(new { message = "Propiedad inválida" });

            var created = await _service.CreateAsync(property);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Property property)
        {
            var updated = await _service.UpdateAsync(id, property);
            return updated ? Ok(updated) : NotFound(new { message = "Propiedad no encontrada" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok(new { message = "Propiedad eliminada" }) : NotFound(new { message = "Propiedad no encontrada" });
        }
    }
}
