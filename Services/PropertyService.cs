using MongoDB.Driver;
using MongoDB.Bson;
using RealEstate.API.Models;
using RealEstate.API.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.JsonPatch;

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

            // Crear índices para mejorar las búsquedas
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

        public async Task<CreatePropertyDto> CreateAsync(Property property)
        {
            await _properties.InsertOneAsync(property);

            // Asignar IdProperty con el Id generado por MongoDB si es null
            // if (string.IsNullOrEmpty(property.IdProperty))
            // {
                property.IdProperty = property.Id;
                var update = Builders<Property>.Update.Set(p => p.IdProperty, property.IdProperty);
                await _properties.UpdateOneAsync(p => p.Id == property.Id, update);
            // }

            return MapToCreateDto(property);
        }

        public async Task<Property?> UpdateAsync(string id, Property updatedProperty)
        {
            var existing = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return null;

            updatedProperty.Id = existing.Id;

            // Mantener IdProperty si no viene en el update
            if (string.IsNullOrEmpty(updatedProperty.IdProperty))
                updatedProperty.IdProperty = existing.IdProperty;

            await _properties.ReplaceOneAsync(p => p.Id == id, updatedProperty);

            return updatedProperty;
        }

        public async Task<PropertyDto?> PatchAsync(string id, JsonPatchDocument<PropertyDto> patchDoc)
        {
            if (!ObjectId.TryParse(id, out _))
                throw new Exception($"El id '{id}' no es un ObjectId válido.");

            var existing = await GetByIdAsync(id);
            if (existing == null)
                return null;

            patchDoc.ApplyTo(existing);

            var updatedModel = MapFromDto(existing);

            // Mantener IdProperty si no viene en el patch
            if (string.IsNullOrEmpty(updatedModel.IdProperty))
                updatedModel.IdProperty = existing.IdProperty;

            var result = await _properties.ReplaceOneAsync(p => p.Id == id, updatedModel);

            return result.ModifiedCount > 0 ? existing : null;
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
        // Mapeos
        // -------------------
        private static PropertyDto MapToDto(Property property)
        {
            return new PropertyDto
            {
                IdProperty = property.IdProperty ?? property.Id,
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


        private static CreatePropertyDto MapToCreateDto(Property property)
        {
            return new CreatePropertyDto
            {
                IdProperty = property.IdProperty ?? property.Id,
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

        private static Property MapFromDto(PropertyDto dto)
        {
            return new Property
            {
                IdProperty = dto.IdProperty,
                Name = dto.Name,
                Address = dto.Address,
                Price = dto.Price,
                CodeInternal = dto.CodeInternal,
                Year = dto.Year,
                Owner = dto.Owner != null ? new Owner
                {
                    IdOwner = dto.Owner.IdOwner,
                    Name = dto.Owner.Name,
                    Address = dto.Owner.Address,
                    Photo = dto.Owner.Photo,
                    Birthday = dto.Owner.Birthday
                } : null,
                Images = dto.Images?.Select(img => new PropertyImage
                {
                    IdPropertyImage = img.IdPropertyImage,
                    File = img.File,
                    Enabled = img.Enabled
                }).ToList(),
                Traces = dto.Traces?.Select(trace => new PropertyTrace
                {
                    IdPropertyTrace = trace.IdPropertyTrace,
                    DateSale = trace.DateSale,
                    Name = trace.Name,
                    Value = trace.Value,
                    Tax = trace.Tax
                }).ToList()
            };
        }
    }
}
