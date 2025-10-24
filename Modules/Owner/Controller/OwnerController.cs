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
        public OwnerController(OwnerService service) => _service = service;

        // ===========================================================
        // GET: api/owner?name=x&address=y
        // ===========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? name, [FromQuery] string? address)
        {
            var result = await _service.GetAsync(name, address);
            return result == null
                ? NotFound(new { message = "No se encontraron propietarios con los criterios dados" })
                : Ok(result);
        }

        // ===========================================================
        // GET: api/owner/{id}
        // ===========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var owner = await _service.GetByIdAsync(id);
            return owner == null
                ? NotFound(new { message = "Propietario no encontrado" })
                : Ok(owner);
        }

        // ===========================================================
        // POST: api/owner
        // ===========================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OwnerDto owner)
        {
            var id = await _service.CreateAsync(owner);
            return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
        }

        // ===========================================================
        // PUT: api/owner/{id}
        // ===========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] OwnerDto owner)
        {
            var result = await _service.UpdateAsync(id, owner);
            if (!result.IsValid)
            {
                if (result.Errors.Any(e => e.PropertyName == "Id"))
                    return NotFound(new { message = "Propietario no encontrado" });
                return BadRequest(result.Errors.Select(e => e.ErrorMessage));
            }

            var updated = await _service.GetByIdAsync(id);
            return Ok(updated);
        }

        // ===========================================================
        // PATCH: api/owner/{id}
        // ===========================================================
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] Dictionary<string, object> fields)
        {
            if (fields == null || fields.Count == 0)
                return BadRequest(new { message = "No se enviaron campos para actualizar" });

            var updated = await _service.PatchAsync(id, fields);
            return updated == null
                ? NotFound(new { message = "Propietario no encontrado" })
                : Ok(updated);
        }

        // ===========================================================
        // DELETE: api/owner/{id}
        // ===========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            return !deleted
                ? NotFound(new { message = "Propietario no encontrado" })
                : NoContent();
        }
    }
}
