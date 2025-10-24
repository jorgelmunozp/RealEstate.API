using FluentValidation;
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

        public PropertyImageService(IMongoDatabase database, IValidator<PropertyImageDto> validator, IConfiguration config)
        {
            var collection = config["MONGO_COLLECTION_PROPERTYIMAGE"]
                ?? throw new Exception("MONGO_COLLECTION_PROPERTYIMAGE no definida");

            _images = database.GetCollection<PropertyImageModel>(collection);
            _validator = validator;
        }

        // 🔹 Obtener todas las imágenes con filtros y paginación
        public async Task<IEnumerable<PropertyImageDto>> GetAllAsync(
            string? idProperty = null,
            bool? enabled = null,
            int page = 1,
            int limit = 6)
        {
            var filter = Builders<PropertyImageModel>.Filter.Empty;

            if (!string.IsNullOrEmpty(idProperty))
                filter &= Builders<PropertyImageModel>.Filter.Eq(i => i.IdProperty, idProperty);

            if (enabled.HasValue)
                filter &= Builders<PropertyImageModel>.Filter.Eq(i => i.Enabled, enabled.Value);

            var images = await _images.Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            return images.Select(PropertyImageMapper.ToDto);
        }

        // 🔹 Obtener imagen por Id
        public async Task<PropertyImageDto?> GetByIdAsync(string id)
        {
            var image = await _images.Find(p => p.Id == id).FirstOrDefaultAsync();
            return image != null ? PropertyImageMapper.ToDto(image) : null;
        }

        // 🔹 Obtener imagen por Id de propiedad
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
            return model.Id;
        }

        // 🔹 Actualizar imagen (PUT)
        public async Task<ValidationResult> UpdateAsync(string id, PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid) return result;

            var existing = await _images.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("Id", "Imagen de propiedad no encontrada"));
                return result;
            }

            var model = image.ToModel();
            model.Id = id; // conservar ID original

            await _images.ReplaceOneAsync(p => p.Id == id, model);
            return result;
        }

        // 🔹 Actualización parcial (PATCH)
        public async Task<ValidationResult> UpdatePartialAsync(string id, PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            var existing = await _images.Find(p => p.Id == id).FirstOrDefaultAsync();

            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("Id", "Imagen no encontrada"));
                return result;
            }

            // Actualiza solo los campos no nulos o no vacíos
            if (image.File != null)
                existing.File = image.File;

            if (!string.IsNullOrEmpty(image.IdProperty))
                existing.IdProperty = image.IdProperty;

            existing.Enabled = image.Enabled; // boolean, siempre actualizable

            await _images.ReplaceOneAsync(p => p.Id == id, existing);
            return result;
        }

        // 🔹 Eliminar imagen
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _images.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
