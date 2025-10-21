using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.PropertyTrace.Dto;

namespace RealEstate.API.Modules.PropertyTrace.Service
{
    public class PropertyTraceService
    {
        private readonly IMongoCollection<PropertyTraceDto> _traces;
        private readonly IValidator<PropertyTraceDto> _validator;

        public PropertyTraceService(IMongoDatabase database, IValidator<PropertyTraceDto> validator, IConfiguration config)
        {
          var collection = config["MONGO_COLLECTION_PROPERTYTRACE"]
                        ?? throw new Exception("MONGO_COLLECTION_PROPERTYTRACE no definida");

            _traces = database.GetCollection<PropertyTraceDto>(collection);
            _validator = validator;
        }

        public async Task<List<PropertyTraceDto>> GetAllAsync() =>
            await _traces.Find(_ => true).ToListAsync();

        public async Task<PropertyTraceDto?> GetByIdAsync(string id) =>
            await _traces.Find(p => p.IdPropertyTrace == id).FirstOrDefaultAsync();

        public async Task<ValidationResult> CreateAsync(PropertyTraceDto trace)
        {
            var result = await _validator.ValidateAsync(trace);
            if (!result.IsValid) return result;

            await _traces.InsertOneAsync(trace);
            return result;
        }

        public async Task<ValidationResult> UpdateAsync(string id, PropertyTraceDto trace)
        {
            var result = await _validator.ValidateAsync(trace);
            if (!result.IsValid) return result;

            var updateResult = await _traces.ReplaceOneAsync(p => p.IdPropertyTrace == id, trace);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("IdPropertyTrace", "Registro de propiedad no encontrado"));

            return result;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _traces.DeleteOneAsync(p => p.IdPropertyTrace == id);
            return result.DeletedCount > 0;
        }
    }
}
