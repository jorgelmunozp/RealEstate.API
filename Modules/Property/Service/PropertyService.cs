using MongoDB.Bson;
using MongoDB.Driver;
using FluentValidation;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Mapper;
using Microsoft.Extensions.Caching.Memory;
using RealEstate.API.Infraestructure.Core.Logs;
using System.Linq;

namespace RealEstate.API.Modules.Property.Service
{
    public class PropertyService
    {
        private readonly IMongoCollection<PropertyModel> _properties;
        private readonly IValidator<PropertyDto> _validator;
        private readonly IMemoryCache _cache;

        public PropertyService(IMongoDatabase database, IValidator<PropertyDto> validator, IConfiguration config, IMemoryCache cache)
        {
            var collection = config["MONGO_COLLECTION_PROPERTY"] ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida");
            _properties = database.GetCollection<PropertyModel>(collection);
            _validator = validator;
            _cache = cache;
        }

        // ===========================================================
        // GET BY ID
        // ===========================================================
        public async Task<PropertyDto?> GetByIdAsync(string id)
        {
            var property = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            return property != null ? property.ToDto() : null;
        }

        // ===========================================================
        // CREATE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<PropertyDto>> CreateAsync(PropertyDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return ServiceLogResponseWrapper<PropertyDto>.Fail("Errores de validación", validation.Errors.Select(e => e.ErrorMessage));

            var model = dto.ToModel();
            await _properties.InsertOneAsync(model);

            return ServiceLogResponseWrapper<PropertyDto>.Ok(model.ToDto(), "Propiedad creada exitosamente", 201);
        }

        // ===========================================================
        // UPDATE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<PropertyDto>> UpdateAsync(string id, PropertyDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return ServiceLogResponseWrapper<PropertyDto>.Fail("Errores de validación", validation.Errors.Select(e => e.ErrorMessage));

            var existing = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceLogResponseWrapper<PropertyDto>.Fail("Propiedad no encontrada", statusCode: 404);

            // Actualiza campos editables
            existing.Name = dto.Name;
            existing.Address = dto.Address;
            existing.Price = dto.Price;
            existing.CodeInternal = dto.CodeInternal;
            existing.Year = dto.Year;
            existing.IdOwner = dto.IdOwner;

            await _properties.ReplaceOneAsync(p => p.Id == id, existing);

            return ServiceLogResponseWrapper<PropertyDto>.Ok(existing.ToDto(), "Propiedad actualizada correctamente");
        }

        // ===========================================================
        // PATCH (actualización parcial real)
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<PropertyDto>> PatchAsync(string id, Dictionary<string, object> fields)
        {
            var existing = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceLogResponseWrapper<PropertyDto>.Fail("Propiedad no encontrada", statusCode: 404);

            var updates = new List<UpdateDefinition<PropertyModel>>();
            var builder = Builders<PropertyModel>.Update;

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Key)) continue;
                var propInfo = typeof(PropertyModel).GetProperty(field.Key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (propInfo != null)
                    updates.Add(builder.Set(propInfo.Name, BsonValue.Create(field.Value)));
            }

            if (!updates.Any())
                return ServiceLogResponseWrapper<PropertyDto>.Fail("No se encontraron campos válidos para actualizar", statusCode: 400);

            var update = builder.Combine(updates);
            await _properties.UpdateOneAsync(p => p.Id == id, update);

            var updated = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            return ServiceLogResponseWrapper<PropertyDto>.Ok(updated.ToDto(), "Propiedad actualizada parcialmente");
        }

        // ===========================================================
        // DELETE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<bool>> DeleteAsync(string id)
        {
            var result = await _properties.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount == 0
                ? ServiceLogResponseWrapper<bool>.Fail("Propiedad no encontrada", statusCode: 404)
                : ServiceLogResponseWrapper<bool>.Ok(true, "Propiedad eliminada correctamente");
        }

        // ===========================================================
        // GET con filtros y caché
        // ===========================================================
        public async Task<object> GetCachedAsync(string? name, string? address, string? idOwner, long? minPrice, long? maxPrice, int page = 1, int limit = 6)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var cacheKey = $"{name}-{address}-{idOwner}-{minPrice}-{maxPrice}-{page}-{limit}";
            if (_cache.TryGetValue(cacheKey, out object cached)) return cached;

            var (data, totalItems) = await GetAllWithMetaAsync(name, address, idOwner, minPrice, maxPrice, page, limit);
            var result = new
            {
                data = data.Select(p => new
                {
                    p.IdProperty,
                    p.Name,
                    p.Address,
                    p.Price,
                    p.Year,
                    p.CodeInternal,
                    p.IdOwner
                }).ToList(),
                meta = new
                {
                    page,
                    limit,
                    total = totalItems,
                    last_page = (int)Math.Ceiling((double)totalItems / limit)
                }
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        // ===========================================================
        // Helper con metadatos
        // ===========================================================
        private async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync(string? name, string? address, string? idOwner, long? minPrice, long? maxPrice, int page = 1, int limit = 6)
        {
            var filterBuilder = Builders<PropertyModel>.Filter;
            var filters = new List<FilterDefinition<PropertyModel>>();

            if (!string.IsNullOrEmpty(name)) filters.Add(filterBuilder.Regex(p => p.Name, new BsonRegularExpression(name, "i")));
            if (!string.IsNullOrEmpty(address)) filters.Add(filterBuilder.Regex(p => p.Address, new BsonRegularExpression(address, "i")));
            if (!string.IsNullOrEmpty(idOwner)) filters.Add(filterBuilder.Eq(p => p.IdOwner, idOwner));
            if (minPrice.HasValue) filters.Add(filterBuilder.Gte(p => p.Price, minPrice.Value));
            if (maxPrice.HasValue) filters.Add(filterBuilder.Lte(p => p.Price, maxPrice.Value));

            var filter = filters.Count > 0 ? filterBuilder.And(filters) : FilterDefinition<PropertyModel>.Empty;

            var totalItems = await _properties.CountDocumentsAsync(filter);
            var data = await _properties.Find(filter).Skip((page - 1) * limit).Limit(limit).ToListAsync();

            return (PropertyMapper.ToDtoList(data), totalItems);
        }
    }
}
