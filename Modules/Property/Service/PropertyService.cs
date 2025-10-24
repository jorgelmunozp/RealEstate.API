using MongoDB.Bson;
using MongoDB.Driver;
using FluentValidation;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Mapper;
using Microsoft.Extensions.Caching.Memory;
using RealEstate.API.Infraestructure.Core.Logs; // donde tengas ServiceLogResponseWrapper<T>
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
            var collection = config["MONGO_COLLECTION_PROPERTY"]
                        ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida");

            _properties = database.GetCollection<PropertyModel>(collection);
            _validator = validator;
            _cache = cache;

            // Índices para optimizar búsquedas
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
        // GET ALL
        // ===========================================================
        public async Task<List<PropertyDto>> GetAllAsync()
        {
            var properties = await _properties.Find(_ => true).ToListAsync();
            return properties.Select(PropertyMapper.ToDto).ToList();
        }

        // ===========================================================
        // GET BY ID
        // ===========================================================
        public async Task<PropertyDto?> GetByIdAsync(string id)
        {
            var property = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            return property != null ? PropertyMapper.ToDto(property) : null;
        }

        // ===========================================================
        // CREATE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<PropertyDto>> CreateAsync(PropertyDto dto)
        {
            try
            {
                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    return ServiceLogResponseWrapper<PropertyDto>.Fail(
                        "Errores de validación",
                        validation.Errors.Select(e => e.ErrorMessage)
                    );
                }

                var model = dto.ToModel();
                await _properties.InsertOneAsync(model);

                return ServiceLogResponseWrapper<PropertyDto>.Ok(model.ToDto(), "Propiedad creada exitosamente", 201);
            }
            catch (Exception ex)
            {
                return ServiceLogResponseWrapper<PropertyDto>.Fail(
                    $"Error interno al crear la propiedad: {ex.Message}",
                    statusCode: 500
                );
            }
        }

        // ===========================================================
        // UPDATE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<PropertyDto>> UpdateAsync(string id, PropertyDto property)
        {
            try
            {
                var validation = await _validator.ValidateAsync(property);
                if (!validation.IsValid)
                {
                    return ServiceLogResponseWrapper<PropertyDto>.Fail(
                        "Errores de validación",
                        validation.Errors.Select(e => e.ErrorMessage)
                    );
                }

                var existing = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (existing == null)
                    return ServiceLogResponseWrapper<PropertyDto>.Fail("Propiedad no encontrada", statusCode: 404);

                // Actualizar solo campos editables
                existing.Name = property.Name;
                existing.Address = property.Address;
                existing.Price = property.Price;
                existing.CodeInternal = property.CodeInternal;
                existing.Year = property.Year;
                existing.IdOwner = property.IdOwner;

                var result = await _properties.ReplaceOneAsync(p => p.Id == id, existing);
                if (result.MatchedCount == 0)
                    return ServiceLogResponseWrapper<PropertyDto>.Fail("No se encontró la propiedad para actualizar", statusCode: 404);

                return ServiceLogResponseWrapper<PropertyDto>.Ok(existing.ToDto(), "Propiedad actualizada correctamente");
            }
            catch (Exception ex)
            {
                return ServiceLogResponseWrapper<PropertyDto>.Fail(
                    $"Error interno al actualizar la propiedad: {ex.Message}",
                    statusCode: 500
                );
            }
        }

        // ===========================================================
        // PATCH (actualización parcial)
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<PropertyDto>> PatchAsync(string id, PropertyDto dto)
        {
            // En este caso reutilizamos UpdateAsync, ya que el frontend envía objeto plano
            return await UpdateAsync(id, dto);
        }

        // ===========================================================
        // DELETE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<bool>> DeleteAsync(string id)
        {
            try
            {
                var result = await _properties.DeleteOneAsync(p => p.Id == id);

                if (result.DeletedCount == 0)
                    return ServiceLogResponseWrapper<bool>.Fail("Propiedad no encontrada", statusCode: 404);

                return ServiceLogResponseWrapper<bool>.Ok(true, "Propiedad eliminada correctamente");
            }
            catch (Exception ex)
            {
                return ServiceLogResponseWrapper<bool>.Fail(
                    $"Error interno al eliminar la propiedad: {ex.Message}",
                    statusCode: 500
                );
            }
        }

        // ===========================================================
        // CACHED GET (filtros + paginación)
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
                data = data.Select(p => new
                {
                    idProperty = p.IdProperty,
                    name = p.Name,
                    address = p.Address,
                    price = p.Price,
                    year = p.Year,
                    codeInternal = p.CodeInternal,
                    idOwner = p.IdOwner
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
        // LISTADO CON METADATOS
        // ===========================================================
        private async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync(
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

            return (PropertyMapper.ToDtoList(data), totalItems);
        }
    }
}
