using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Model;
using RealEstate.API.Modules.PropertyImage.Mapper;

namespace RealEstate.API.Modules.PropertyImage.Service
{
    public class PropertyImageService
    {
        private readonly IMongoCollection<PropertyImageModel> _images;
        private readonly IValidator<PropertyImageDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTtl;

        public PropertyImageService(IMongoDatabase database, IValidator<PropertyImageDto> validator, IConfiguration config, IMemoryCache cache)
        {
            var collection = config["MONGO_COLLECTION_PROPERTYIMAGE"]
                ?? throw new Exception("MONGO_COLLECTION_PROPERTYIMAGE no definida");

            _images = database.GetCollection<PropertyImageModel>(collection);
            _validator = validator;
            _cache = cache;

            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = (int.TryParse(ttlStr, out var m) && m > 0)
                ? TimeSpan.FromMinutes(m)
                : TimeSpan.FromMinutes(5);
        }

        // ===========================================================
        // 🔹 GET ALL con filtros, paginación y caché
        // ===========================================================
        public async Task<IEnumerable<PropertyImageDto>> GetAllAsync(
            string? idProperty = null,
            bool? enabled = null,
            int page = 1,
            int limit = 6,
            bool refresh = false)
        {
            var cacheKey = $"pimg:{idProperty}-{enabled}-{page}-{limit}";
            if (!refresh && _cache.TryGetValue(cacheKey, out List<PropertyImageDto>? cached))
                return cached!;

            var filter = Builders<PropertyImageModel>.Filter.Empty;

            if (!string.IsNullOrEmpty(idProperty))
                filter &= Builders<PropertyImageModel>.Filter.Eq(i => i.IdProperty, idProperty);

            if (enabled.HasValue)
                filter &= Builders<PropertyImageModel>.Filter.Eq(i => i.Enabled, enabled.Value);

            var images = await _images.Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            var result = images.Select(PropertyImageMapper.ToDto).ToList();

            if (!refresh)
                _cache.Set(cacheKey, result, _cacheTtl);

            return result;
        }

        // ===========================================================
        // 🔹 GET BY ID
        // ===========================================================
        public async Task<PropertyImageDto?> GetByIdAsync(string idPropertyImage)
        {
            var image = await _images.Find(p => p.Id == idPropertyImage).FirstOrDefaultAsync();
            return image != null ? PropertyImageMapper.ToDto(image) : null;
        }

        // ===========================================================
        // 🔹 GET BY PROPERTY ID
        // ===========================================================
        public async Task<PropertyImageDto?> GetByPropertyIdAsync(string propertyId)
        {
            var image = await _images.Find(i => i.IdProperty == propertyId).FirstOrDefaultAsync();
            return image != null ? PropertyImageMapper.ToDto(image) : null;
        }

        // ===========================================================
        // 🔹 CREATE (POST)
        // ===========================================================
        public async Task<string> CreateAsync(PropertyImageDto image)
        {
            if (image == null)
                throw new ValidationException("El cuerpo de la solicitud no puede estar vacío");

            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);

            // Evita duplicar si ya existe una imagen para la propiedad
            if (!string.IsNullOrEmpty(image.IdProperty))
            {
                var existing = await _images.Find(i => i.IdProperty == image.IdProperty).FirstOrDefaultAsync();
                if (existing != null)
                {
                    // 🔹 Si ya existe, se actualiza en vez de crear nueva
                    var update = Builders<PropertyImageModel>.Update
                        .Set(i => i.File, image.File)
                        .Set(i => i.Enabled, image.Enabled);
                    await _images.UpdateOneAsync(i => i.Id == existing.Id, update);
                    return existing.Id;
                }
            }

            var model = image.ToModel();
            await _images.InsertOneAsync(model);
            return model.Id;
        }

        // ===========================================================
        // 🔹 UPDATE COMPLETO (PUT)
        // ===========================================================
        public async Task<ValidationResult> UpdateAsync(string idPropertyImage, PropertyImageDto image)
        {
            image.IdPropertyImage = idPropertyImage;
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid) return result;

            var existing = await _images.Find(p => p.Id == idPropertyImage).FirstOrDefaultAsync();
            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("IdPropertyImage", "Imagen no encontrada"));
                return result;
            }

            var model = image.ToModel();
            model.Id = existing.Id;

            await _images.ReplaceOneAsync(p => p.Id == existing.Id, model);
            return result;
        }

        // ===========================================================
        // 🔹 UPDATE PARCIAL (PATCH)
        // ===========================================================
        public async Task<ValidationResult> UpdatePartialAsync(string idPropertyImage, PropertyImageDto image)
        {
            image.IdPropertyImage = idPropertyImage;
            var result = await _validator.ValidateAsync(image);

            var existing = await _images.Find(p => p.Id == idPropertyImage).FirstOrDefaultAsync();
            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("IdPropertyImage", "Imagen no encontrada"));
                return result;
            }

            // Solo actualiza campos enviados
            if (!string.IsNullOrEmpty(image.File))
                existing.File = image.File;
            if (!string.IsNullOrEmpty(image.IdProperty))
                existing.IdProperty = image.IdProperty;

            existing.Enabled = image.Enabled;

            await _images.ReplaceOneAsync(p => p.Id == existing.Id, existing);
            return result;
        }

        // ===========================================================
        // 🔹 DELETE
        // ===========================================================
        public async Task<bool> DeleteAsync(string idPropertyImage)
        {
            var result = await _images.DeleteOneAsync(p => p.Id == idPropertyImage);
            return result.DeletedCount > 0;
        }
    }
}
