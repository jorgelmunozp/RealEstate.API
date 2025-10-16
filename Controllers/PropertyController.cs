using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Services;

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

        // 🔹 Obtener todas las propiedades (con filtros, paginación y metadatos)
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

        // 🔹 Obtener una propiedad específica por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var property = await _service.GetByIdAsync(id);
            return property != null ? Ok(property) : NotFound(new { message = "Propiedad no encontrada" });
        }
    }
}
