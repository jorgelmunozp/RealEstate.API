using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.Properties.Service;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using Microsoft.AspNetCore.Authorization;

namespace RealEstate.API.Modules.Properties.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly PropertiesService _service;

        public PropertiesController(PropertiesService service)
        {
            _service = service;
        }

        // ===========================================================
        // GET: api/Property
        // ===========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? address,
            [FromQuery] long? minPrice,
            [FromQuery] long? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 6)
        {
            var result = await _service.GetCachedAsync(name, address, minPrice, maxPrice, page, limit);
            return Ok(result);
        }

        // ===========================================================
        // GET: api/Property/{id}
        // ===========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
                return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(dto);
        }

        // ===========================================================
        // POST: api/Property
        // ===========================================================
        [HttpPost]
        [Authorize]
        // public async Task<IActionResult> Create([FromBody] PropertyDto dto)
        // {
        //     // Mapear DTO a modelo
        //     var property = MapDtoToProperty(dto);

        //     // Guardar en la base de datos usando el método asíncrono CreateAsync en PropertyService
        //     var created = await _service.CreateAsync(property);

        //     return CreatedAtAction(nameof(GetById), new { id = created.IdProperty }, created);
        // }


        // ===========================================================
        // PUT: api/Property/{id}
        // ===========================================================
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyDto dto)
        {
            // Mapear DTO a modelo
            var property = MapDtoToProperty(dto);

            // Guardar cambios en la base de datos
            var updated = await _service.UpdateAsync(id, property);
            if (updated == null) return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(updated);
        }

        // ===========================================================
        // PATCH: api/Property/{id}
        // ===========================================================
        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> Patch(string id, [FromBody] JsonPatchDocument<PropertyDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest(new { message = "Documento PATCH inválido" });

            var existingDto = await _service.GetByIdAsync(id);
            if (existingDto == null)
                return NotFound(new { message = "Propiedad no encontrada" });

            // Aplicar cambios al DTO
            patchDoc.ApplyTo(existingDto, e =>
            {
                ModelState.AddModelError(e.AffectedObject?.ToString() ?? string.Empty, e.ErrorMessage);
            });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Mapear a modelo
            var updatedProperty = MapDtoToProperty(existingDto);
            updatedProperty.Id = id;

            // Guardar cambios en la base de datos
            var result = await _service.UpdateAsync(id, updatedProperty);
            return Ok(result);
        }

        // ===========================================================
        // DELETE: api/Property/{id}
        // ===========================================================
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(new { message = "Propiedad eliminada" });
        }

        // ===========================================================
        // Mapeos DTO → Modelo
        // ===========================================================
        private PropertyModel MapDtoToProperty(PropertyDto dto)
        {
            return new PropertyModel
            {
                Name = dto.Name,
                Address = dto.Address,
                Price = dto.Price,
                CodeInternal = dto.CodeInternal,
                Year = dto.Year,
                IdOwner = dto.IdOwner,
            };
        }
    }
}
