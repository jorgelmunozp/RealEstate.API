using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.PropertyImage.Dto;

namespace RealEstate.API.Modules.PropertyImage.Service
{
    public class PropertyImageService
    {
        private readonly IMongoCollection<PropertyImageDto> _images;
        private readonly IValidator<PropertyImageDto> _validator;

        public PropertyImageService(IMongoDatabase database, IValidator<PropertyImageDto> validator, IConfiguration config)
        {
           var collection = config["MONGO_COLLECTION_PROPERTYIMAGE"]
                        ?? throw new Exception("MONGO_COLLECTION_PROPERTYIMAGE no definida");

            _images = database.GetCollection<PropertyImageDto>(collection);
            _validator = validator;
        }

        // Obtener todas las imágenes
        public async Task<List<PropertyImageDto>> GetAllAsync() =>
            await _images.Find(_ => true).ToListAsync();

        // Obtener imagen por Id
        public async Task<PropertyImageDto?> GetByIdAsync(string id) =>
            await _images.Find(p => p.IdPropertyImage == id).FirstOrDefaultAsync();

        // Obtener imágenes por Id de propiedad
        public async Task<List<PropertyImageDto>> GetByPropertyIdAsync(string propertyId) =>
            await _images.Find(p => p.IdProperty == propertyId).ToListAsync();

        // Crear nueva imagen
        public async Task<ValidationResult> CreateAsync(PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid) return result;

            await _images.InsertOneAsync(image);
            return result;
        }

        // Actualizar imagen
        public async Task<ValidationResult> UpdateAsync(string id, PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid) return result;

            var updateResult = await _images.ReplaceOneAsync(p => p.IdPropertyImage == id, image);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("IdPropertyImage", "Imagen de propiedad no encontrada"));

            return result;
        }

        // Eliminar imagen
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _images.DeleteOneAsync(p => p.IdPropertyImage == id);
            return result.DeletedCount > 0;
        }
    }
}
