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

        // GET: api/propertyimage
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        // GET: api/propertyimage/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var image = await _service.GetByIdAsync(id);
            if (image == null) return NotFound(new { Message = "Imagen no encontrada" });
            return Ok(image);
        }

        // GET: api/propertyimage/property/{propertyId}
        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetByPropertyId(string propertyId) =>
            Ok(await _service.GetByPropertyIdAsync(propertyId));

        // POST: api/propertyimage
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyImageDto image)
        {
            var id = await _service.CreateAsync(image);
            return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
        }

        // PUT: api/propertyimage/{id}
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

            var updatedImage = await _service.GetByIdAsync(id);
            return Ok(updatedImage);
        }

        // DELETE: api/propertyimage/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            bool deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { Message = "Imagen no encontrada" });

            return NoContent();
        }
    }
}
