using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using FluentValidation.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Model;
using RealEstate.API.Modules.Owner.Mapper;

namespace RealEstate.API.Modules.Owner.Service
{
    public class OwnerService
    {
        private readonly IMongoCollection<OwnerModel> _owners;
        private readonly IValidator<OwnerDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTtl;

        public OwnerService(IMongoDatabase database, IValidator<OwnerDto> validator, IConfiguration config, IMemoryCache cache)
        {
            var collection = config["MONGO_COLLECTION_OWNER"]
                ?? throw new Exception("MONGO_COLLECTION_OWNER no definida");

            _owners = database.GetCollection<OwnerModel>(collection);
            _validator = validator;
            _cache = cache;
            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = (int.TryParse(ttlStr, out var m) && m > 0) ? TimeSpan.FromMinutes(m) : TimeSpan.FromMinutes(5);
        }

        // ===========================================================
        // GET: con filtros opcionales
        // ===========================================================
        public async Task<List<OwnerDto>> GetAsync(string? name = null, string? address = null, bool refresh = false)
        {
            var cacheKey = $"owner:{name}-{address}";
            if (!refresh)
            {
                var cached = _cache.Get<List<OwnerDto>>(cacheKey);
                if (cached != null) return cached;
            }

            var filter = Builders<OwnerModel>.Filter.Empty;

            if (!string.IsNullOrEmpty(name))
                filter &= Builders<OwnerModel>.Filter.Regex(o => o.Name, new BsonRegularExpression(name, "i"));

            if (!string.IsNullOrEmpty(address))
                filter &= Builders<OwnerModel>.Filter.Regex(o => o.Address, new BsonRegularExpression(address, "i"));

            var owners = await _owners.Find(filter).ToListAsync();
            var result = owners.Select(OwnerMapper.ToDto).ToList();
            _cache.Set(cacheKey, result, _cacheTtl);
            return result;
        }

        // ===========================================================
        // GET BY ID
        // ===========================================================
        public async Task<OwnerDto?> GetByIdAsync(string id)
        {
            var owner = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            return owner != null ? OwnerMapper.ToDto(owner) : null;
        }

        // ===========================================================
        // CREATE
        // ===========================================================
        public async Task<string> CreateAsync(OwnerDto owner)
        {
            var result = await _validator.ValidateAsync(owner);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);

            var model = owner.ToModel();
            await _owners.InsertOneAsync(model);
            return model.Id;
        }

        // ===========================================================
        // UPDATE (PUT)
        // ===========================================================
        public async Task<ValidationResult> UpdateAsync(string id, OwnerDto owner)
        {
            var result = await _validator.ValidateAsync(owner);
            if (!result.IsValid) return result;

            var existing = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("Id", "Propietario no encontrado"));
                return result;
            }

            var model = owner.ToModel();
            model.Id = id;

            await _owners.ReplaceOneAsync(o => o.Id == id, model);
            return result;
        }

        // ===========================================================
        // PATCH parcial (solo campos enviados)
        // ===========================================================
        public async Task<OwnerDto?> PatchAsync(string id, Dictionary<string, object> fields)
        {
            var existing = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            if (existing == null) return null;

            var updates = new List<UpdateDefinition<OwnerModel>>();
            var builder = Builders<OwnerModel>.Update;

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Key)) continue;
                var propInfo = typeof(OwnerModel).GetProperty(field.Key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (propInfo != null)
                    updates.Add(builder.Set(propInfo.Name, BsonValue.Create(field.Value)));
            }

            if (!updates.Any()) return null;

            var update = builder.Combine(updates);
            await _owners.UpdateOneAsync(o => o.Id == id, update);

            var updated = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            return updated != null ? OwnerMapper.ToDto(updated) : null;
        }

        // ===========================================================
        // DELETE
        // ===========================================================
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _owners.DeleteOneAsync(o => o.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
