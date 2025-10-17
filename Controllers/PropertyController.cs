using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Services;
using RealEstate.API.Models;
using RealEstate.API.Dtos;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/Property
        // [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? name,
            [FromQuery] string? address,
            [FromQuery] long? minPrice,
            [FromQuery] long? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            var result = await _service.GetCachedAsync(name, address, minPrice, maxPrice, page, limit);
            return Ok(result);
        }

        // GET: api/Property/{id}
        // [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
                return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(dto);
        }

        // POST: api/Property
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePropertyDto dto)
        {
            var property = MapCreateDtoToProperty(dto);

            // No es necesario asignar IdProperty aquí, lo hace el service
            var created = await _service.CreateAsync(property);

            return CreatedAtAction(nameof(GetById), new { id = created.IdProperty }, created);
        }

        // PUT: api/Property/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyDto dto)
        {
            var property = MapDtoToProperty(dto);
            var updated = await _service.UpdateAsync(id, property);
            if (updated == null)
                return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(updated);
        }

        // PATCH: api/Property/{id}
        [Authorize]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] JsonPatchDocument<PropertyDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest(new { message = "Documento PATCH inválido" });

            var existingDto = await _service.GetByIdAsync(id);
            if (existingDto == null)
                return NotFound(new { message = "Propiedad no encontrada" });

            patchDoc.ApplyTo(existingDto, e =>
            {
                ModelState.AddModelError(e.AffectedObject?.ToString() ?? string.Empty, e.ErrorMessage);
            });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedProperty = MapDtoToProperty(existingDto);

            // ✅ Mantener el Id original ANTES de guardar
            updatedProperty.Id = id;

            var result = await _service.UpdateAsync(id, updatedProperty);
            return Ok(result);
        }

        // DELETE: api/Property/{id}
        [Authorize]
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Propiedad no encontrada" });

            return Ok(new { message = "Propiedad eliminada" });
        }

        // -------------------
        // Mapeo DTO -> Modelo
        // -------------------
        private Property MapDtoToProperty(PropertyDto dto)
        {
            return new Property
            {
                IdProperty = dto.IdProperty,
                Name = dto.Name,
                Address = dto.Address,
                Price = dto.Price,
                CodeInternal = dto.CodeInternal,
                Year = dto.Year,
                Owner = dto.Owner != null ? new Owner
                {
                    IdOwner = dto.Owner.IdOwner,
                    Name = dto.Owner.Name,
                    Address = dto.Owner.Address,
                    Photo = dto.Owner.Photo,
                    Birthday = dto.Owner.Birthday
                } : null,
                Images = dto.Images?.Select(img => new PropertyImage
                {
                    IdPropertyImage = img.IdPropertyImage,
                    File = img.File,
                    Enabled = img.Enabled
                }).ToList(),
                Traces = dto.Traces?.Select(trace => new PropertyTrace
                {
                    IdPropertyTrace = trace.IdPropertyTrace,
                    DateSale = trace.DateSale,
                    Name = trace.Name,
                    Value = trace.Value,
                    Tax = trace.Tax
                }).ToList()
            };
        }

        private Property MapCreateDtoToProperty(CreatePropertyDto dto)
        {
            return new Property
            {
                Name = dto.Name,
                Address = dto.Address,
                Price = dto.Price,
                CodeInternal = dto.CodeInternal,
                Year = dto.Year,
                Owner = dto.Owner != null ? new Owner
                {
                    IdOwner = dto.Owner.IdOwner,
                    Name = dto.Owner.Name,
                    Address = dto.Owner.Address,
                    Photo = dto.Owner.Photo,
                    Birthday = dto.Owner.Birthday
                } : null,
                Images = dto.Images?.Select(img => new PropertyImage
                {
                    IdPropertyImage = img.IdPropertyImage,
                    File = img.File,
                    Enabled = img.Enabled
                }).ToList(),
                Traces = dto.Traces?.Select(trace => new PropertyTrace
                {
                    IdPropertyTrace = trace.IdPropertyTrace,
                    DateSale = trace.DateSale,
                    Name = trace.Name,
                    Value = trace.Value,
                    Tax = trace.Tax
                }).ToList()
            };
        }
    }
}
