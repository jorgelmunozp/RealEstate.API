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
            _cacheTtl = (int.TryParse(ttlStr, out var m) && m > 0) ? TimeSpan.FromMinutes(m) : TimeSpan.FromMinutes(5);
        }

        // 🔹 Obtener todas las imágenes con filtros y paginación
        public async Task<IEnumerable<PropertyImageDto>> GetAllAsync(
            string? idProperty = null,
            bool? enabled = null,
            int page = 1,
            int limit = 6,
            bool refresh = false)
        {
            var cacheKey = $"pimg:{idProperty}-{enabled}-{page}-{limit}";
            if (!refresh)
            {
                var cached = _cache.Get<List<PropertyImageDto>>(cacheKey);
                if (cached != null) return cached;
            }

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
            _cache.Set(cacheKey, result, _cacheTtl);
            return result;
        }

        // 🔹 Obtener imagen por IdPropertyImage
        public async Task<PropertyImageDto?> GetByIdAsync(string idPropertyImage)
        {
            var image = await _images.Find(p => p.IdPropertyImage == idPropertyImage).FirstOrDefaultAsync();
            return image != null ? PropertyImageMapper.ToDto(image) : null;
        }

        // 🔹 Obtener imagen por IdProperty
        public async Task<PropertyImageDto?> GetByPropertyIdAsync(string propertyId)
        {
            var image = await _images.Find(i => i.IdProperty == propertyId).FirstOrDefaultAsync();
            return image != null ? PropertyImageMapper.ToDto(image) : null;
        }

        // 🔹 Crear nueva imagen
        public async Task<string> CreateAsync(PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);

            var model = image.ToModel();
            await _images.InsertOneAsync(model);
            return model.IdPropertyImage;
        }

        // 🔹 Actualizar imagen (PUT)
        public async Task<ValidationResult> UpdateAsync(string idPropertyImage, PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid) return result;

            var existing = await _images.Find(p => p.IdPropertyImage == idPropertyImage).FirstOrDefaultAsync();
            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("IdPropertyImage", "Imagen de propiedad no encontrada"));
                return result;
            }

            var model = image.ToModel();
            model.IdPropertyImage = idPropertyImage;

            await _images.ReplaceOneAsync(p => p.IdPropertyImage == idPropertyImage, model);
            return result;
        }

        // 🔹 Actualización parcial (PATCH)
        public async Task<ValidationResult> UpdatePartialAsync(string idPropertyImage, PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            var existing = await _images.Find(p => p.IdPropertyImage == idPropertyImage).FirstOrDefaultAsync();

            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("IdPropertyImage", "Imagen no encontrada"));
                return result;
            }

            if (!string.IsNullOrEmpty(image.File))
                existing.File = image.File;

            if (!string.IsNullOrEmpty(image.IdProperty))
                existing.IdProperty = image.IdProperty;

            existing.Enabled = image.Enabled;

            await _images.ReplaceOneAsync(p => p.IdPropertyImage == idPropertyImage, existing);
            return result;
        }

        // 🔹 Eliminar imagen
        public async Task<bool> DeleteAsync(string idPropertyImage)
        {
            var result = await _images.DeleteOneAsync(p => p.IdPropertyImage == idPropertyImage);
            return result.DeletedCount > 0;
        }
    }
}
