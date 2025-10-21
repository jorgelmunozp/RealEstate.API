using Microsoft.AspNetCore.Mvc;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Service;

namespace RealEstate.API.Modules.Owner.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class OwnerController : ControllerBase
    {
        private readonly OwnerService _service;

        public OwnerController(OwnerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var owner = await _service.GetByIdAsync(id);
            if (owner == null) return NotFound(new { Message = "Propietario no encontrado" });
            return Ok(owner);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OwnerDto owner)
        {
            var result = await _service.CreateAsync(owner);
            if (!result.IsValid) return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            return CreatedAtAction(nameof(GetById), new { id = owner.IdOwner }, owner);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] OwnerDto owner)
        {
            var result = await _service.UpdateAsync(id, owner);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "IdOwner"))
                    return NotFound(new { Message = "Propietario no encontrado" });
                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }
            return Ok(await _service.GetByIdAsync(id));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { Message = "Propietario no encontrado" });
            return NoContent();
        }
    }
}
