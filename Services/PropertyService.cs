using MongoDB.Driver;
using MongoDB.Bson;
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

            // Índices
            _properties.Indexes.CreateOne(
                new CreateIndexModel<Property>(
                    Builders<Property>.IndexKeys
                        .Ascending(p => p.Name)
                        .Ascending(p => p.Address)
                        .Ascending(p => p.Price)
                )
            );
        }

        // GET paginado y cacheado
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

        // Filtros + paginación
        public async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync(string? name, string? address, decimal? minPrice, decimal? maxPrice, int page = 1, int limit = 10)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var filterBuilder = Builders<Property>.Filter;
            var filters = new List<FilterDefinition<Property>>();

            if (!string.IsNullOrEmpty(name))
                filters.Add(filterBuilder.Regex(nameof(Property.Name), new BsonRegularExpression(name, "i")));
            if (!string.IsNullOrEmpty(address))
                filters.Add(filterBuilder.Regex(nameof(Property.Address), new BsonRegularExpression(address, "i")));
            if (minPrice.HasValue)
                filters.Add(filterBuilder.Gte(nameof(Property.Price), minPrice.Value));
            if (maxPrice.HasValue)
                filters.Add(filterBuilder.Lte(nameof(Property.Price), maxPrice.Value));

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

        // VALIDACIÓN de ObjectId antes de buscar
        public async Task<PropertyDto?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                throw new Exception($"El id '{id}' no es un ObjectId válido.");

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
            if (!ObjectId.TryParse(id, out _))
                throw new Exception($"El id '{id}' no es un ObjectId válido.");

            var result = await _properties.ReplaceOneAsync(p => p.Id == id, property);
            return result.ModifiedCount > 0 ? MapToDto(property) : null;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                throw new Exception($"El id '{id}' no es un ObjectId válido.");

            var result = await _properties.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return false;

            var count = await _properties.CountDocumentsAsync(p => p.Id == id);
            return count > 0;
        }

        // -------------------
        // Map manual Property → DTO
        // -------------------
        private static PropertyDto MapToDto(Property property)
        {
            return new PropertyDto
            {
                IdProperty = property.Id,
                Name = property.Name,
                Address = property.Address,
                Price = property.Price,
                CodeInternal = property.CodeInternal,
                Year = property.Year,
                Owner = property.Owner != null ? new OwnerDto
                {
                    IdOwner = property.Owner.IdOwner,
                    Name = property.Owner.Name,
                    Address = property.Owner.Address,
                    Photo = property.Owner.Photo,
                    Birthday = property.Owner.Birthday
                } : new OwnerDto(),
                Images = property.Images?.Select(img => new PropertyImageDto
                {
                    IdPropertyImage = img.IdPropertyImage,
                    File = img.File,
                    Enabled = img.Enabled
                }).ToList() ?? new List<PropertyImageDto>(),
                Traces = property.Traces?.Select(trace => new PropertyTraceDto
                {
                    IdPropertyTrace = trace.IdPropertyTrace,
                    DateSale = trace.DateSale,
                    Name = trace.Name,
                    Value = trace.Value,
                    Tax = trace.Tax
                }).ToList() ?? new List<PropertyTraceDto>()
            };
        }
    }
}
