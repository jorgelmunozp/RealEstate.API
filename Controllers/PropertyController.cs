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

        /// <summary>
        /// Obtiene todas las propiedades.
        /// </summary>
        /// <param name="name">Filtrar por nombre de propiedad (opcional).</param>
        /// <param name="address">Filtrar por dirección (opcional).</param>
        /// <param name="minPrice">Filtrar por precio mínimo (opcional).</param>
        /// <param name="maxPrice">Filtrar por precio máximo (opcional).</param>
        /// <param name="page">Número de página para paginación (por defecto 1).</param>
        /// <param name="limit">Cantidad de elementos por página (por defecto 10).</param>
        /// <returns>Lista paginada de propiedades que cumplen los filtros.</returns>
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

        /// <summary>
        /// Obtiene una propiedad específica por su ID.
        /// </summary>
        /// <param name="id">ID de la propiedad.</param>
        /// <returns>Propiedad encontrada o mensaje de error si no existe.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var property = await _service.GetByIdAsync(id);
            return property != null ? Ok(property) : NotFound(new { message = "Propiedad no encontrada" });
        }

        /// <summary>
        /// Crea una nueva propiedad.
        /// </summary>
        /// <param name="property">Objeto de la propiedad a crear.</param>
        /// <returns>La propiedad creada.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Property property)
        {
            if (property == null)
                return BadRequest(new { message = "Propiedad inválida" });

            var created = await _service.CreateAsync(property);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Actualiza una propiedad existente.
        /// </summary>
        /// <param name="id">ID de la propiedad a actualizar.</param>
        /// <param name="property">Objeto con los datos a actualizar.</param>
        /// <returns>Propiedad actualizada o mensaje de error si no existe.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Property property)
        {
            var updated = await _service.UpdateAsync(id, property);
            return updated != null ? Ok(updated) : NotFound(new { message = "Propiedad no encontrada" });
        }

        /// <summary>
        /// Elimina una propiedad por su ID.
        /// </summary>
        /// <param name="id">ID de la propiedad a eliminar.</param>
        /// <returns>Mensaje de confirmación o error si no existe.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok(new { message = "Propiedad eliminada" }) : NotFound(new { message = "Propiedad no encontrada" });
        }
    }
}
