using MongoDB.Bson;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Mapper;
using Microsoft.Extensions.Caching.Memory;

namespace RealEstate.API.Modules.Property.Service
{
    public class PropertyService
    {
        private readonly IMongoCollection<PropertyModel> _properties;
        private readonly IValidator<PropertyDto> _validator;
        private readonly IMemoryCache _cache;

        public PropertyService(IMongoDatabase database, IValidator<PropertyDto> validator, IConfiguration config, IMemoryCache cache)
        {
            var collection = config["MONGO_COLLECTION_PROPERTY"]
                        ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida");

            _properties = database.GetCollection<PropertyModel>(collection);
            _validator = validator;
            _cache = cache;

            // Crear índices para mejorar búsquedas
            _properties.Indexes.CreateOne(
                new CreateIndexModel<PropertyModel>(
                    Builders<PropertyModel>.IndexKeys
                        .Ascending(p => p.Name)
                        .Ascending(p => p.Address)
                        .Ascending(p => p.Price)
                )
            );
        }

        // ===========================================================
        // 🔹 Obtener todas las propiedades (simple)
        // ===========================================================
        public async Task<List<PropertyDto>> GetAllAsync()
        {
            var properties = await _properties.Find(_ => true).ToListAsync();
            return properties.Select(p => PropertyMapper.ToDto(p)).ToList();
        }

        // ===========================================================
        // 🔹 Obtener propiedad por Id
        // ===========================================================
        public async Task<PropertyDto?> GetByIdAsync(string id)
        {
            var property = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            return property != null ? PropertyMapper.ToDto(property) : null;
        }
        // ===========================================================
        // 🔹 Crear nueva propiedad
        // ===========================================================
        public async Task<string> CreateAsync(PropertyDto property)
        {
            var result = await _validator.ValidateAsync(property);
            if (!result.IsValid) throw new ValidationException(result.Errors);

            var model = property.ToModel();
            await _properties.InsertOneAsync(model);
            return model.Id;
        }

        // ===========================================================
        // 🔹 Actualizar propiedad
        // ===========================================================
        public async Task<ValidationResult> UpdateAsync(string Id, PropertyDto property)
        {
            var result = await _validator.ValidateAsync(property);
            if (!result.IsValid) return result;

            var model = property.ToModel();
            var updateResult = await _properties.ReplaceOneAsync(p => p.Id == Id, model);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("Id", "Propiedad no encontrada"));

            return result;
        }

        // ===========================================================
        // 🔹 Eliminar propiedad
        // ===========================================================
        public async Task<bool> DeleteAsync(string Id)
        {
            var result = await _properties.DeleteOneAsync(p => p.Id == Id);
            return result.DeletedCount > 0;
        }

        // ===========================================================
        // 🔹 Obtener con filtros, paginación y caché
        // ===========================================================
        public async Task<object> GetCachedAsync(
            string? name, string? address, string? idOwner,
            long? minPrice, long? maxPrice,
            int page = 1, int limit = 6)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var cacheKey = $"{name}-{address}-{idOwner}-{minPrice}-{maxPrice}-{page}-{limit}";
            if (_cache.TryGetValue(cacheKey, out object cached))
                return cached;

            var (data, totalItems) = await GetAllWithMetaAsync(name, address, idOwner, minPrice, maxPrice, page, limit);

    var result = new
    {
        data = data, // tu lista de propiedades
        meta = new
        {
            page = page,
            limit = limit,
            total = totalItems,
            last_page = (int)Math.Ceiling((double)totalItems / limit)
        }
    };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        // ===========================================================
        // 🔹 Obtener lista con metadatos
        // ===========================================================
        public async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync(
            string? name, string? address, string? idOwner,
            long? minPrice, long? maxPrice,
            int page = 1, int limit = 6)
        {
            var filterBuilder = Builders<PropertyModel>.Filter;
            var filters = new List<FilterDefinition<PropertyModel>>();

            if (!string.IsNullOrEmpty(name))
                filters.Add(filterBuilder.Regex(p => p.Name, new BsonRegularExpression(name, "i")));

            if (!string.IsNullOrEmpty(address))
                filters.Add(filterBuilder.Regex(p => p.Address, new BsonRegularExpression(address, "i")));

            if (!string.IsNullOrEmpty(idOwner))
                filters.Add(filterBuilder.Eq(p => p.IdOwner, idOwner));

            if (minPrice.HasValue)
                filters.Add(filterBuilder.Gte(p => p.Price, minPrice.Value));

            if (maxPrice.HasValue)
                filters.Add(filterBuilder.Lte(p => p.Price, maxPrice.Value));

            var filter = filters.Count > 0 ? filterBuilder.And(filters) : FilterDefinition<PropertyModel>.Empty;

            var totalItems = await _properties.CountDocumentsAsync(filter);

            var data = await _properties
                .Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            var dataDto = PropertyMapper.ToDtoList(data);

            return (dataDto, totalItems);
        }
    }
}
