using MongoDB.Bson;
using MongoDB.Driver;
using FluentValidation;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Mapper;
using RealEstate.API.Modules.PropertyImage.Model;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Service;
using Microsoft.Extensions.Caching.Memory;
using RealEstate.API.Infraestructure.Core.Logs;

namespace RealEstate.API.Modules.Property.Service
{
    public class PropertyService
    {
        private readonly IMongoCollection<PropertyModel> _properties;
        private readonly IMongoCollection<PropertyImageModel> _images;
        private readonly IValidator<PropertyDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTtl;
        private readonly PropertyImageService _imageService;

        public PropertyService(
            IMongoDatabase database,
            IValidator<PropertyDto> validator,
            IConfiguration config,
            IMemoryCache cache,
            PropertyImageService imageService // 🔹 Inyección del servicio de imágenes
        )
        {
            var propertyCollection = config["MONGO_COLLECTION_PROPERTY"] ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida");
            var imageCollection = config["MONGO_COLLECTION_PROPERTYIMAGE"] ?? throw new Exception("MONGO_COLLECTION_PROPERTYIMAGE no definida");

            _properties = database.GetCollection<PropertyModel>(propertyCollection);
            _images = database.GetCollection<PropertyImageModel>(imageCollection);
            _validator = validator;
            _cache = cache;
            _imageService = imageService;

            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = int.TryParse(ttlStr, out var minutes) && minutes > 0
                ? TimeSpan.FromMinutes(minutes)
                : TimeSpan.FromMinutes(5);
        }

        // ===========================================================
        // GET BY ID
        // ===========================================================
        public async Task<PropertyDto?> GetByIdAsync(string id)
        {
            var property = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (property == null) return null;

            var image = await _images.Find(i => i.IdProperty == property.Id).FirstOrDefaultAsync();
            var dto = property.ToDto();

            if (image != null)
            {
                dto.Image = new PropertyImageDto
                {
                    IdPropertyImage = image.Id,
                    IdProperty = image.IdProperty,
                    File = image.File,
                    Enabled = image.Enabled
                };
            }

            return dto;
        }

        // ===========================================================
        // CREATE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<PropertyDto>> CreateAsync(PropertyDto dto)
        {
            var validation = await _validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return ServiceLogResponseWrapper<PropertyDto>.Fail("Errores de validación", validation.Errors.Select(e => e.ErrorMessage));

            // 1️⃣ Crear propiedad
            var model = dto.ToModel();
            await _properties.InsertOneAsync(model);

            // 2️⃣ Si la propiedad viene con imagen embebida, guardarla mediante PropertyImageService
            if (dto.Image != null && !string.IsNullOrWhiteSpace(dto.Image.File))
            {
                dto.Image.IdProperty = model.Id;
                await _imageService.CreateAsync(dto.Image);
            }

            // 3️⃣ Retornar propiedad con imagen
            var created = await GetByIdAsync(model.Id);
            return ServiceLogResponseWrapper<PropertyDto>.Ok(created!, "Propiedad e imagen creadas exitosamente", 201);
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

            // 🔹 Actualizar campos principales
            existing.Name = dto.Name;
            existing.Address = dto.Address;
            existing.Price = dto.Price;
            existing.CodeInternal = dto.CodeInternal;
            existing.Year = dto.Year;
            existing.IdOwner = dto.IdOwner;

            await _properties.ReplaceOneAsync(p => p.Id == id, existing);

            // 🔹 Si vino imagen embebida, actualizar o crear usando PropertyImageService
            if (dto.Image != null && !string.IsNullOrWhiteSpace(dto.Image.File))
            {
                var existingImage = await _images.Find(i => i.IdProperty == id).FirstOrDefaultAsync();
                dto.Image.IdProperty = id;

                if (existingImage != null)
                    await _imageService.UpdateAsync(existingImage.Id, dto.Image);
                else
                    await _imageService.CreateAsync(dto.Image);
            }

            var updated = await GetByIdAsync(id);
            return ServiceLogResponseWrapper<PropertyDto>.Ok(updated!, "Propiedad actualizada correctamente");
        }

        // ===========================================================
        // PATCH
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

            await _properties.UpdateOneAsync(p => p.Id == id, builder.Combine(updates));

            var updated = await GetByIdAsync(id);
            return ServiceLogResponseWrapper<PropertyDto>.Ok(updated!, "Propiedad actualizada parcialmente");
        }

        // ===========================================================
        // DELETE
        // ===========================================================
        public async Task<ServiceLogResponseWrapper<bool>> DeleteAsync(string id)
        {
            var result = await _properties.DeleteOneAsync(p => p.Id == id);
            await _images.DeleteManyAsync(i => i.IdProperty == id); // 🔹 limpia imágenes asociadas

            return result.DeletedCount == 0
                ? ServiceLogResponseWrapper<bool>.Fail("Propiedad no encontrada", statusCode: 404)
                : ServiceLogResponseWrapper<bool>.Ok(true, "Propiedad eliminada correctamente");
        }

        // ===========================================================
        // GET con filtros y caché
        // ===========================================================
        public async Task<object> GetCachedAsync(string? name, string? address, string? idOwner, long? minPrice, long? maxPrice, int page = 1, int limit = 6, bool refresh = false)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);
            var cacheKey = $"{name}-{address}-{idOwner}-{minPrice}-{maxPrice}-{page}-{limit}";

            if (!refresh && _cache.TryGetValue(cacheKey, out object? cached))
                return cached!;

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
                    p.IdOwner,
                    p.Image
                }).ToList(),
                meta = new
                {
                    page,
                    limit,
                    total = totalItems,
                    last_page = (int)Math.Ceiling((double)totalItems / limit)
                }
            };

            if (!refresh)
                _cache.Set(cacheKey, result, _cacheTtl);

            return result;
        }

        // ===========================================================
        // Helper con metadatos e imagen
        // ===========================================================
        private async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync(
            string? name, string? address, string? idOwner, long? minPrice, long? maxPrice,
            int page = 1, int limit = 6)
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
            var data = await _properties.Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            var dtoList = new List<PropertyDto>();
            foreach (var prop in data)
            {
                var dto = prop.ToDto();
                var image = await _images.Find(i => i.IdProperty == prop.Id).FirstOrDefaultAsync();
                if (image != null)
                {
                    dto.Image = new PropertyImageDto
                    {
                        IdPropertyImage = image.Id,
                        IdProperty = image.IdProperty,
                        File = image.File,
                        Enabled = image.Enabled
                    };
                }
                dtoList.Add(dto);
            }

            return (dtoList, totalItems);
        }
    }
}
