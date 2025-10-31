using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Model;
using RealEstate.API.Modules.PropertyImage.Interface;

namespace RealEstate.API.Modules.PropertyImage.Service
{
    public class PropertyImageService : IPropertyImageService
    {
        private readonly IMongoCollection<PropertyImageModel> _images;
        private readonly IValidator<PropertyImageDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly TimeSpan _cacheTtl;

        public PropertyImageService(IMongoDatabase database, IValidator<PropertyImageDto> validator, IConfiguration config, IMemoryCache cache, IMapper mapper)
        {
            var collection = config["MONGO_COLLECTION_PROPERTYIMAGE"]
                ?? throw new Exception("MONGO_COLLECTION_PROPERTYIMAGE no definida");

            _images = database.GetCollection<PropertyImageModel>(collection);
            _validator = validator;
            _cache = cache;
            _mapper = mapper;

            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = int.TryParse(ttlStr, out var m) && m > 0
                ? TimeSpan.FromMinutes(m)
                : TimeSpan.FromMinutes(5);
        }

        // GET ALL (con filtros, paginación y caché)
        public async Task<ServiceResultWrapper<IEnumerable<PropertyImageDto>>> GetAllAsync(
            string? idProperty = null,
            bool? enabled = null,
            int page = 1,
            int limit = 6,
            bool refresh = false)
        {
            var cacheKey = $"pimg:{idProperty}-{enabled}-{page}-{limit}";
            if (!refresh && _cache.TryGetValue(cacheKey, out IEnumerable<PropertyImageDto>? cached))
                return ServiceResultWrapper<IEnumerable<PropertyImageDto>>.Ok(cached, "Imágenes obtenidas desde caché");

            var fb = Builders<PropertyImageModel>.Filter;
            var filter = fb.Empty;

            if (!string.IsNullOrWhiteSpace(idProperty))
                filter &= fb.Eq(i => i.IdProperty, idProperty);

            if (enabled.HasValue)
                filter &= fb.Eq(i => i.Enabled, enabled.Value);

            var images = await _images.Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            var result = _mapper.Map<IEnumerable<PropertyImageDto>>(images);
            _cache.Set(cacheKey, result, _cacheTtl);

            return ServiceResultWrapper<IEnumerable<PropertyImageDto>>.Ok(result, "Listado de imágenes obtenido correctamente");
        }

        // GET BY ID
        public async Task<ServiceResultWrapper<PropertyImageDto>> GetByIdAsync(string idPropertyImage)
        {
            var image = await _images.Find(p => p.Id == idPropertyImage).FirstOrDefaultAsync();
            if (image == null)
                return ServiceResultWrapper<PropertyImageDto>.Fail("Imagen no encontrada", 404);

            return ServiceResultWrapper<PropertyImageDto>.Ok(_mapper.Map<PropertyImageDto>(image), "Imagen obtenida correctamente");
        }

        // GET BY PROPERTY ID
        public async Task<PropertyImageDto?> GetByPropertyIdAsync(string propertyId)
        {
            var image = await _images.Find(i => i.IdProperty == propertyId).FirstOrDefaultAsync();
            return image != null ? _mapper.Map<PropertyImageDto>(image) : null;
        }
        
        // CREATE
        public async Task<ServiceResultWrapper<PropertyImageDto>> CreateAsync(PropertyImageDto image)
        {
            if (image == null)
                return ServiceResultWrapper<PropertyImageDto>.Fail("El cuerpo de la solicitud no puede estar vacío", 400);

            var validation = await _validator.ValidateAsync(image);
            if (!validation.IsValid)
                return ServiceResultWrapper<PropertyImageDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400);

            if (!string.IsNullOrWhiteSpace(image.IdProperty))
            {
                var existing = await _images.Find(i => i.IdProperty == image.IdProperty).FirstOrDefaultAsync();
                if (existing != null)
                {
                    var update = Builders<PropertyImageModel>.Update
                        .Set(i => i.File, image.File)
                        .Set(i => i.Enabled, image.Enabled);

                    await _images.UpdateOneAsync(i => i.Id == existing.Id, update);
                    var updated = await _images.Find(i => i.Id == existing.Id).FirstOrDefaultAsync();
                    return ServiceResultWrapper<PropertyImageDto>.Updated(_mapper.Map<PropertyImageDto>(updated), "Imagen actualizada correctamente");
                }
            }

            var model = _mapper.Map<PropertyImageModel>(image);
            await _images.InsertOneAsync(model);
            return ServiceResultWrapper<PropertyImageDto>.Created(_mapper.Map<PropertyImageDto>(model), "Imagen creada correctamente");
        }

        // UPDATE (PUT)
        public async Task<ServiceResultWrapper<PropertyImageDto>> UpdateAsync(string idPropertyImage, PropertyImageDto image)
        {
            image.IdPropertyImage = idPropertyImage;

            var validation = await _validator.ValidateAsync(image);
            if (!validation.IsValid)
                return ServiceResultWrapper<PropertyImageDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400);

            var existing = await _images.Find(p => p.Id == idPropertyImage).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<PropertyImageDto>.Fail("Imagen no encontrada", 404);

            var updatedModel = _mapper.Map(image, existing);
            await _images.ReplaceOneAsync(p => p.Id == existing.Id, updatedModel);

            return ServiceResultWrapper<PropertyImageDto>.Updated(_mapper.Map<PropertyImageDto>(updatedModel), "Imagen actualizada correctamente");
        }

        // PATCH (actualización parcial)
        public async Task<ServiceResultWrapper<PropertyImageDto>> PatchAsync(string idPropertyImage, Dictionary<string, object> fields)
        {
            if (fields == null || fields.Count == 0)
                return ServiceResultWrapper<PropertyImageDto>.Fail("No se enviaron campos válidos", 400);

            var existing = await _images.Find(p => p.Id == idPropertyImage).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<PropertyImageDto>.Fail("Imagen no encontrada", 404);

            var builder = Builders<PropertyImageModel>.Update;
            var updates = new List<UpdateDefinition<PropertyImageModel>>();

            foreach (var f in fields)
            {
                if (!string.IsNullOrWhiteSpace(f.Key))
                    updates.Add(builder.Set(f.Key, f.Value));
            }

            if (!updates.Any())
                return ServiceResultWrapper<PropertyImageDto>.Fail("No se enviaron campos válidos", 400);

            await _images.UpdateOneAsync(p => p.Id == idPropertyImage, builder.Combine(updates));
            var updated = await _images.Find(p => p.Id == idPropertyImage).FirstOrDefaultAsync();

            return ServiceResultWrapper<PropertyImageDto>.Updated(_mapper.Map<PropertyImageDto>(updated), "Imagen actualizada parcialmente");
        }

        // DELETE
        public async Task<ServiceResultWrapper<bool>> DeleteAsync(string idPropertyImage)
        {
            var result = await _images.DeleteOneAsync(p => p.Id == idPropertyImage);
            if (result.DeletedCount == 0)
                return ServiceResultWrapper<bool>.Fail("Imagen no encontrada", 404);

            return ServiceResultWrapper<bool>.Deleted("Imagen eliminada correctamente");
        }
    }
}
