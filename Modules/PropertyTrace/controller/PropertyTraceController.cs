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

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var trace = await _service.GetByIdAsync(id);
            if (trace == null) return NotFound(new { Message = "Registro no encontrado" });
            return Ok(trace);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PropertyTraceDto trace)
        {
            var id = await _service.CreateAsync(trace);
            return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
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
