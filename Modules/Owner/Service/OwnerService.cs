using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Model;

namespace RealEstate.API.Modules.Owner.Service
{
    public class OwnerService
    {
        private readonly IMongoCollection<OwnerModel> _owners;
        private readonly IValidator<OwnerDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly TimeSpan _cacheTtl;

        public OwnerService(IMongoDatabase database, IValidator<OwnerDto> validator, IConfiguration config, IMemoryCache cache, IMapper mapper)
        {
            var collection = config["MONGO_COLLECTION_OWNER"]
                ?? throw new Exception("MONGO_COLLECTION_OWNER no definida");

            _owners = database.GetCollection<OwnerModel>(collection);
            _validator = validator;
            _cache = cache;
            _mapper = mapper;

            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = int.TryParse(ttlStr, out var m) && m > 0
                ? TimeSpan.FromMinutes(m)
                : TimeSpan.FromMinutes(5);
        }

        // ===========================================================
        // GET (con filtros opcionales y caché)
        // ===========================================================
        public async Task<ServiceResultWrapper<List<OwnerDto>>> GetAsync(string? name = null, string? address = null, bool refresh = false)
        {
            var cacheKey = $"owner:{name}-{address}";
            if (!refresh && _cache.TryGetValue(cacheKey, out List<OwnerDto>? cached))
                return ServiceResultWrapper<List<OwnerDto>>.Ok(cached!, "Propietarios obtenidos desde caché");

            var fb = Builders<OwnerModel>.Filter;
            var filter = fb.Empty;

            if (!string.IsNullOrEmpty(name))
                filter &= fb.Regex(o => o.Name, new BsonRegularExpression(name, "i"));

            if (!string.IsNullOrEmpty(address))
                filter &= fb.Regex(o => o.Address, new BsonRegularExpression(address, "i"));

            var owners = await _owners.Find(filter).ToListAsync();
            var result = _mapper.Map<List<OwnerDto>>(owners);

            _cache.Set(cacheKey, result, _cacheTtl);
            return ServiceResultWrapper<List<OwnerDto>>.Ok(result, "Propietarios obtenidos correctamente");
        }

        // ===========================================================
        // GET BY ID
        // ===========================================================
        public async Task<ServiceResultWrapper<OwnerDto>> GetByIdAsync(string id)
        {
            var owner = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (owner == null)
                return ServiceResultWrapper<OwnerDto>.Fail("Propietario no encontrado", 404);

            return ServiceResultWrapper<OwnerDto>.Ok(_mapper.Map<OwnerDto>(owner), "Propietario obtenido correctamente");
        }

        // ===========================================================
        // CREATE
        // ===========================================================
        public async Task<ServiceResultWrapper<OwnerDto>> CreateAsync(OwnerDto owner)
        {
            var validation = await _validator.ValidateAsync(owner);
            if (!validation.IsValid)
                return ServiceResultWrapper<OwnerDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400);

            var model = _mapper.Map<OwnerModel>(owner);
            await _owners.InsertOneAsync(model);

            _cache.Remove("owner:all");
            return ServiceResultWrapper<OwnerDto>.Created(_mapper.Map<OwnerDto>(model), "Propietario creado correctamente");
        }

        // ===========================================================
        // UPDATE (PUT)
        // ===========================================================
        public async Task<ServiceResultWrapper<OwnerDto>> UpdateAsync(string id, OwnerDto owner)
        {
            var validation = await _validator.ValidateAsync(owner);
            if (!validation.IsValid)
                return ServiceResultWrapper<OwnerDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400);

            var existing = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<OwnerDto>.Fail("Propietario no encontrado", 404);

            var updatedModel = _mapper.Map(owner, existing);
            await _owners.ReplaceOneAsync(o => o.Id == id, updatedModel);

            _cache.Remove("owner:all");
            _cache.Remove($"owner:{id}");

            return ServiceResultWrapper<OwnerDto>.Updated(_mapper.Map<OwnerDto>(updatedModel), "Propietario actualizado correctamente");
        }

        // ===========================================================
        // PATCH (actualización parcial)
        // ===========================================================
        public async Task<ServiceResultWrapper<OwnerDto>> PatchAsync(string id, Dictionary<string, object> fields)
        {
            if (fields == null || fields.Count == 0)
                return ServiceResultWrapper<OwnerDto>.Fail("No se enviaron campos válidos para actualizar", 400);

            var existing = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<OwnerDto>.Fail("Propietario no encontrado", 404);

            var builder = Builders<OwnerModel>.Update;
            var updates = new List<UpdateDefinition<OwnerModel>>();

            foreach (var field in fields)
            {
                var prop = typeof(OwnerModel).GetProperty(field.Key,
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (prop != null)
                    updates.Add(builder.Set(prop.Name, BsonValue.Create(field.Value)));
            }

            if (!updates.Any())
                return ServiceResultWrapper<OwnerDto>.Fail("Sin cambios válidos para aplicar", 400);

            await _owners.UpdateOneAsync(o => o.Id == id, builder.Combine(updates));

            var updated = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (updated == null)
                return ServiceResultWrapper<OwnerDto>.Fail("Error al actualizar propietario", 500);

            _cache.Remove("owner:all");
            _cache.Remove($"owner:{id}");

            return ServiceResultWrapper<OwnerDto>.Updated(_mapper.Map<OwnerDto>(updated), "Propietario actualizado parcialmente");
        }

        // ===========================================================
        // DELETE
        // ===========================================================
        public async Task<ServiceResultWrapper<bool>> DeleteAsync(string id)
        {
            var result = await _owners.DeleteOneAsync(o => o.Id == id);
            if (result.DeletedCount == 0)
                return ServiceResultWrapper<bool>.Fail("Propietario no encontrado", 404);

            _cache.Remove("owner:all");
            _cache.Remove($"owner:{id}");

            return ServiceResultWrapper<bool>.Deleted("Propietario eliminado correctamente");
        }
    }
}
