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

        // 🔹 GET api/propertytrace?idProperty=xxxx
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? idProperty)
        {
            var traces = await _service.GetAllAsync(idProperty);
            return Ok(traces);
        }

        // 🔹 GET api/propertytrace/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var trace = await _service.GetByIdAsync(id);
            if (trace == null)
                return NotFound(new { Message = "Registro no encontrado" });

            return Ok(trace);
        }

        // 🔹 POST api/propertytrace
        // Recibe lista de DTOs (una o varias)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IEnumerable<PropertyTraceDto> traces)
        {
            if (traces == null || !traces.Any())
                return BadRequest(new { Message = "No se enviaron trazas válidas" });

            var ids = await _service.CreateAsync(traces);
            return CreatedAtAction(nameof(GetAll), new { }, new { Ids = ids });
        }

        // 🔹 PUT api/propertytrace/{id}
        // Reemplaza el registro completo
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

            var updated = await _service.GetByIdAsync(id);
            return Ok(updated);
        }

        // 🔹 PATCH api/propertytrace/{id}
        // Actualización parcial (ideal para frontend moderno)
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] PropertyTraceDto trace)
        {
            var result = await _service.UpdatePartialAsync(id, trace);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "Id"))
                    return NotFound(new { Message = "Registro no encontrado" });
                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updated = await _service.GetByIdAsync(id);
            return Ok(updated);
        }

        // 🔹 DELETE api/propertytrace/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { Message = "Registro no encontrado" });

            return NoContent();
        }
    }
}
