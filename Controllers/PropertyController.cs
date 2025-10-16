using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Services;
using RealEstate.API.Models;
using RealEstate.API.Dtos;

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
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
                return NotFound(new { message = "Propiedad no encontrada" });
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Propiedad inválida" });

            var property = new Property
            {
                IdOwner = dto.IdOwner,
                Name = dto.Name,
                AddressProperty = dto.AddressProperty,
                PriceProperty = dto.PriceProperty,
                ImageUrl = dto.ImageUrl
            };

            var created = await _service.CreateAsync(property);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Propiedad inválida" });

            var property = new Property
            {
                Id = dto.Id,
                IdOwner = dto.IdOwner,
                Name = dto.Name,
                AddressProperty = dto.AddressProperty,
                PriceProperty = dto.PriceProperty,
                ImageUrl = dto.ImageUrl
            };

            var updated = await _service.UpdateAsync(id, property);
            if (updated == null)
                return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok(new { message = "Propiedad eliminada" }) : NotFound(new { message = "Propiedad no encontrada" });
        }
    }
}
