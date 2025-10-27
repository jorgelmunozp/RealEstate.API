using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Model;

namespace RealEstate.API.Modules.PropertyTrace.Service
{
    public class PropertyTraceService
    {
        private readonly IMongoCollection<PropertyTraceModel> _traces;
        private readonly IValidator<PropertyTraceDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly TimeSpan _cacheTtl;

        public PropertyTraceService(IMongoDatabase database, IValidator<PropertyTraceDto> validator, IConfiguration config, IMemoryCache cache, IMapper mapper)
        {
            var collection = config["MONGO_COLLECTION_PROPERTYTRACE"]
                ?? throw new Exception("MONGO_COLLECTION_PROPERTYTRACE no definida");

            _traces = database.GetCollection<PropertyTraceModel>(collection);
            _validator = validator;
            _cache = cache;
            _mapper = mapper;

            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = int.TryParse(ttlStr, out var m) && m > 0
                ? TimeSpan.FromMinutes(m)
                : TimeSpan.FromMinutes(5);
        }

        // ===========================================================
        // GET ALL (con filtro opcional por propiedad y caché)
        // ===========================================================
        public async Task<ServiceResultWrapper<IEnumerable<PropertyTraceDto>>> GetAllAsync(string? idProperty = null, bool refresh = false)
        {
            var cacheKey = $"ptrace:{idProperty ?? "all"}";
            if (!refresh && _cache.TryGetValue(cacheKey, out IEnumerable<PropertyTraceDto>? cached))
                return ServiceResultWrapper<IEnumerable<PropertyTraceDto>>.Ok(cached, "Trazas obtenidas desde caché");

            var filter = string.IsNullOrWhiteSpace(idProperty)
                ? Builders<PropertyTraceModel>.Filter.Empty
                : Builders<PropertyTraceModel>.Filter.Eq(t => t.IdProperty, idProperty);

            var traces = await _traces.Find(filter).ToListAsync();
            var result = _mapper.Map<IEnumerable<PropertyTraceDto>>(traces);

            _cache.Set(cacheKey, result, _cacheTtl);
            return ServiceResultWrapper<IEnumerable<PropertyTraceDto>>.Ok(result, "Listado de trazas obtenido correctamente");
        }

        // ===========================================================
        // GET BY ID
        // ===========================================================
        public async Task<ServiceResultWrapper<PropertyTraceDto>> GetByIdAsync(string id)
        {
            var trace = await _traces.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (trace == null)
                return ServiceResultWrapper<PropertyTraceDto>.Fail("Traza no encontrada", 404);

            return ServiceResultWrapper<PropertyTraceDto>.Ok(_mapper.Map<PropertyTraceDto>(trace), "Traza obtenida correctamente");
        }

        // ===========================================================
        // CREATE (una o varias trazas)
        // ===========================================================
        public async Task<ServiceResultWrapper<List<string>>> CreateAsync(IEnumerable<PropertyTraceDto> traces)
        {
            var ids = new List<string>();
            var errors = new List<string>();

            foreach (var dto in traces)
            {
                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    errors.AddRange(validation.Errors.Select(e => e.ErrorMessage));
                    continue;
                }

                var model = _mapper.Map<PropertyTraceModel>(dto);
                await _traces.InsertOneAsync(model);
                ids.Add(model.Id);
            }

            if (errors.Any())
                return ServiceResultWrapper<List<string>>.Fail(errors, 400, "Algunas trazas no fueron válidas");

            return ServiceResultWrapper<List<string>>.Created(ids, "Trazas creadas correctamente");
        }

        // ===========================================================
        // CREATE (una sola traza)
        // ===========================================================
        public async Task<ServiceResultWrapper<PropertyTraceDto>> CreateSingleAsync(PropertyTraceDto trace)
        {
            var validation = await _validator.ValidateAsync(trace);
            if (!validation.IsValid)
                return ServiceResultWrapper<PropertyTraceDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400);

            var model = _mapper.Map<PropertyTraceModel>(trace);
            await _traces.InsertOneAsync(model);

            return ServiceResultWrapper<PropertyTraceDto>.Created(_mapper.Map<PropertyTraceDto>(model), "Traza creada correctamente");
        }

        // ===========================================================
        // UPDATE (PUT)
        // ===========================================================
        public async Task<ServiceResultWrapper<PropertyTraceDto>> UpdateAsync(string id, PropertyTraceDto trace)
        {
            var validation = await _validator.ValidateAsync(trace);
            if (!validation.IsValid)
                return ServiceResultWrapper<PropertyTraceDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400);

            var existing = await _traces.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<PropertyTraceDto>.Fail("Traza no encontrada", 404);

            var updatedModel = _mapper.Map(trace, existing);
            await _traces.ReplaceOneAsync(t => t.Id == id, updatedModel);

            return ServiceResultWrapper<PropertyTraceDto>.Updated(_mapper.Map<PropertyTraceDto>(updatedModel), "Traza actualizada correctamente");
        }

        // ===========================================================
        // PATCH (actualización parcial)
        // ===========================================================
        public async Task<ServiceResultWrapper<PropertyTraceDto>> PatchAsync(string id, Dictionary<string, object> fields)
        {
            if (fields == null || fields.Count == 0)
                return ServiceResultWrapper<PropertyTraceDto>.Fail("No se enviaron campos para actualizar", 400);

            var existing = await _traces.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<PropertyTraceDto>.Fail("Traza no encontrada", 404);

            var updates = new List<UpdateDefinition<PropertyTraceModel>>();
            var builder = Builders<PropertyTraceModel>.Update;

            foreach (var field in fields)
            {
                if (!string.IsNullOrWhiteSpace(field.Key))
                    updates.Add(builder.Set(field.Key, field.Value));
            }

            if (!updates.Any())
                return ServiceResultWrapper<PropertyTraceDto>.Fail("No se enviaron campos válidos", 400);

            await _traces.UpdateOneAsync(t => t.Id == id, builder.Combine(updates));

            var updated = await _traces.Find(t => t.Id == id).FirstOrDefaultAsync();
            return ServiceResultWrapper<PropertyTraceDto>.Updated(_mapper.Map<PropertyTraceDto>(updated), "Traza actualizada parcialmente");
        }

        // ===========================================================
        // DELETE
        // ===========================================================
        public async Task<ServiceResultWrapper<bool>> DeleteAsync(string id)
        {
            var result = await _traces.DeleteOneAsync(t => t.Id == id);
            if (result.DeletedCount == 0)
                return ServiceResultWrapper<bool>.Fail("Traza no encontrada", 404);

            return ServiceResultWrapper<bool>.Deleted("Traza eliminada correctamente");
        }
    }
}
