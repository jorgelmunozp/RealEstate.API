using MongoDB.Driver;
using RealEstate.API.Models;
using RealEstate.API.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace RealEstate.API.Services
{
    public class PropertyService
    {
        private readonly IMongoCollection<Property> _properties;
        private readonly IMemoryCache _cache;

        public PropertyService(IConfiguration config, IMemoryCache cache)
        {
            _cache = cache;

            var connectionString = config["MONGO_CONNECTION"] 
                                   ?? throw new Exception("MONGO_CONNECTION no definida");
            var databaseName = config["MONGO_DATABASE"] 
                               ?? throw new Exception("MONGO_DATABASE no definida");
            var collectionName = config["MONGO_COLLECTION"] 
                                 ?? throw new Exception("MONGO_COLLECTION no definida");

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _properties = database.GetCollection<Property>(collectionName);

            // Crear índices
            _properties.Indexes.CreateOne(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys
                        .Ascending(p => p.Name)
                        .Ascending(p => p.AddressProperty)
                        .Ascending(p => p.PriceProperty)
                )
            );
        }

        // Cache + paginación + metadatos
        public async Task<object> GetCachedAsync(string? name, string? address, decimal? minPrice, decimal? maxPrice, int page = 1, int limit = 10)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var cacheKey = $"{name}-{address}-{minPrice}-{maxPrice}-{page}-{limit}";
            if (_cache.TryGetValue(cacheKey, out object cached))
                return cached;

            var (data, totalItems) = await GetAllWithMetaAsync(name, address, minPrice, maxPrice, page, limit);

            var result = new
            {
                page,
                limit,
                totalItems,
                totalPages = (int)Math.Ceiling((double)totalItems / limit),
                data
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        // Filtros + paginación + conteo total
        public async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync(string? name, string? address, decimal? minPrice, decimal? maxPrice, int page = 1, int limit = 10)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var filterBuilder = Builders<Property>.Filter;
            var filters = new List<FilterDefinition<Property>>();

            if (!string.IsNullOrEmpty(name))
                filters.Add(filterBuilder.Regex(nameof(Property.Name), new MongoDB.Bson.BsonRegularExpression(name, "i")));
            if (!string.IsNullOrEmpty(address))
                filters.Add(filterBuilder.Regex(nameof(Property.AddressProperty), new MongoDB.Bson.BsonRegularExpression(address, "i")));
            if (minPrice.HasValue)
                filters.Add(filterBuilder.Gte(nameof(Property.PriceProperty), minPrice.Value));
            if (maxPrice.HasValue)
                filters.Add(filterBuilder.Lte(nameof(Property.PriceProperty), maxPrice.Value));

            var filter = filters.Count > 0 ? filterBuilder.And(filters) : FilterDefinition<Property>.Empty;

            var totalItems = await _properties.CountDocumentsAsync(filter);
            var dataEntities = await _properties
                .Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            var dataDtos = dataEntities.Select(MapToDto).ToList();
            return (dataDtos, totalItems);
        }

        public async Task<PropertyDto?> GetByIdAsync(string id)
        {
            var property = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            return property != null ? MapToDto(property) : null;
        }

        public async Task<PropertyDto> CreateAsync(Property property)
        {
            await _properties.InsertOneAsync(property);
            return MapToDto(property);
        }

        public async Task<PropertyDto?> UpdateAsync(string id, Property property)
        {
            var result = await _properties.ReplaceOneAsync(p => p.Id == id, property);
            return result.ModifiedCount > 0 ? MapToDto(property) : null;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _properties.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            var count = await _properties.CountDocumentsAsync(p => p.Id == id);
            return count > 0;
        }

        // Mapeo Property → DTO
        private static PropertyDto MapToDto(Property property) => new PropertyDto
        {
            Id = property.Id,
            IdOwner = property.IdOwner,
            Name = property.Name,
            AddressProperty = property.AddressProperty,
            PriceProperty = property.PriceProperty,
            ImageUrl = property.ImageUrl
        };
    }
}
