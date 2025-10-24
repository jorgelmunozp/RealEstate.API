using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Service;

namespace RealEstate.API.Modules.PropertyTrace.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyTraceController : ControllerBase
    {
        private readonly PropertyTraceService _service;

        public PropertyTraceController(PropertyTraceService service)
        {
            _service = service;
        }

        // GET api/propertytrace?idProperty - Devuelve todos los registros como arreglo
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? idProperty)
        {
            var traces = await _service.GetAllAsync(idProperty);
            return Ok(traces);
        }

        // Devuelve un arreglo incluso si es un solo registro
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var trace = await _service.GetByIdAsync(id);
            if (trace == null) return NotFound(new List<PropertyTraceDto>()); // Devuelve arreglo vacío
            return Ok(new List<PropertyTraceDto> { trace }); // Envolver en lista
        }

        // Recibe lista de DTOs
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IEnumerable<PropertyTraceDto> traces)
        {
            var ids = await _service.CreateAsync(traces);
            return CreatedAtAction(nameof(GetById), new { id = ids.FirstOrDefault() }, new { Ids = ids });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] PropertyTraceDto trace)
        {
            var result = await _service.UpdateAsync(id, trace);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "Id"))
                    return NotFound(new { Message = "Registro no encontrado" });
                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }
            return Ok(await _service.GetByIdAsync(id));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { Message = "Registro no encontrado" });
            return NoContent();
        }
    }
}
