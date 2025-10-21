using Microsoft.AspNetCore.Mvc;
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

        // GET: api/property
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        // GET: api/property/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var property = await _service.GetByIdAsync(id);
            if (property == null) return NotFound(new { Message = "Propiedad no encontrada" });
            return Ok(property);
        }

        // POST: api/property
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyDto property)
        {
            var result = await _service.CreateAsync(property);
            if (!result.IsValid) return BadRequest(result.Errors.Select(e => e.ErrorMessage));

            return CreatedAtAction(nameof(GetById), new { id = property.IdProperty }, property);
        }

        // PUT: api/property/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyDto property)
        {
            var result = await _service.UpdateAsync(id, property);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "IdProperty"))
                    return NotFound(new { Message = "Propiedad no encontrada" });

                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updatedProperty = await _service.GetByIdAsync(id);
            return Ok(updatedProperty);
        }

        // DELETE: api/property/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            bool deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { Message = "Propiedad no encontrada" });

            return NoContent();
        }
    }
}
