using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.Property.Dto;

namespace RealEstate.API.Modules.Property.Service
{
    public class PropertyService
    {
        private readonly IMongoCollection<PropertyDto> _properties;
        private readonly IValidator<PropertyDto> _validator;

        public PropertyService(IMongoDatabase database, IValidator<PropertyDto> validator, IConfiguration config)
        {
            var collection = config["MONGO_COLLECTION_PROPERTY"]
                        ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida");

            _properties = database.GetCollection<PropertyDto>(collection);
            _validator = validator;
        }

        // Obtener todas las propiedades
        public async Task<List<PropertyDto>> GetAllAsync() =>
            await _properties.Find(_ => true).ToListAsync();

        // Obtener propiedad por Id
        public async Task<PropertyDto?> GetByIdAsync(string id) =>
            await _properties.Find(p => p.IdProperty == id).FirstOrDefaultAsync();

        // Crear nueva propiedad
        public async Task<ValidationResult> CreateAsync(PropertyDto property)
        {
            var result = await _validator.ValidateAsync(property);
            if (!result.IsValid) return result;

            await _properties.InsertOneAsync(property);
            return result;
        }

        // Actualizar propiedad
        public async Task<ValidationResult> UpdateAsync(string id, PropertyDto property)
        {
            var result = await _validator.ValidateAsync(property);
            if (!result.IsValid) return result;

            var updateResult = await _properties.ReplaceOneAsync(p => p.IdProperty == id, property);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("IdProperty", "Propiedad no encontrada"));

            return result;
        }

        // Eliminar propiedad
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _properties.DeleteOneAsync(p => p.IdProperty == id);
            return result.DeletedCount > 0;
        }
    }
}
