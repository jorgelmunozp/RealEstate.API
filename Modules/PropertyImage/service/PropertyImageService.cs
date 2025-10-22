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

        // Obtener todas las imágenes
        public async Task<List<PropertyImageDto>> GetAllAsync()
        {
            var images = await _images.Find(_ => true).ToListAsync();
            return images.Select(i => PropertyImageMapper.ToDto(i)).ToList();
        }

        // Obtener imagen por Id
        public async Task<PropertyImageDto?> GetByIdAsync(string id)
        {
            var image = await _images.Find(p => p.Id == id).FirstOrDefaultAsync();
            return image != null ? PropertyImageMapper.ToDto(image) : null;
        }


        // Obtener imágenes por Id de propiedad
        public async Task<List<PropertyImageDto>> GetByPropertyIdAsync(string propertyId)
        {
            var images = await _images.Find(p => p.IdProperty == propertyId).ToListAsync();
            return images.Select(i => PropertyImageMapper.ToDto(i)).ToList();
        }
        // Crear nueva imagen
        public async Task<string> CreateAsync(PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid) throw new ValidationException(result.Errors);

            var model = image.ToModel();
            await _images.InsertOneAsync(model);
            return model.Id;            
        }

        // Actualizar imagen
        public async Task<ValidationResult> UpdateAsync(string id, PropertyImageDto image)
        {
            var result = await _validator.ValidateAsync(image);
            if (!result.IsValid) return result;

            var model = image.ToModel();
            var updateResult = await _images.ReplaceOneAsync(p => p.Id == id, model);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("Id", "Imagen de propiedad no encontrada"));

            return result;
        }

        // Eliminar imagen
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _images.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
