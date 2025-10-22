using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Model;
using RealEstate.API.Modules.PropertyTrace.Mapper;

namespace RealEstate.API.Modules.PropertyTrace.Service
{
    public class PropertyTraceService
    {
        private readonly IMongoCollection<PropertyTraceModel> _traces;
        private readonly IValidator<PropertyTraceDto> _validator;

        public PropertyTraceService(IMongoDatabase database, IValidator<PropertyTraceDto> validator, IConfiguration config)
        {
          var collection = config["MONGO_COLLECTION_PROPERTYTRACE"]
                        ?? throw new Exception("MONGO_COLLECTION_PROPERTYTRACE no definida");

            _traces = database.GetCollection<PropertyTraceModel>(collection);
            _validator = validator;
        }

        public async Task<List<PropertyTraceDto>> GetAllAsync()
        {
            var traces = await _traces.Find(_ => true).ToListAsync();
            return traces.Select(t => PropertyTraceMapper.ToDto(t)).ToList();
        }

        public async Task<PropertyTraceDto?> GetByIdAsync(string id)
        {
            var trace = await _traces.Find(p => p.Id == id).FirstOrDefaultAsync();
            return trace != null ? PropertyTraceMapper.ToDto(trace) : null;
        }


        public async Task<string> CreateAsync(PropertyTraceDto trace)
        {
            var result = await _validator.ValidateAsync(trace);
            if (!result.IsValid) throw new ValidationException(result.Errors);
            
            var model = trace.ToModel();
            await _traces.InsertOneAsync(model);
            return model.Id;
        }

        public async Task<ValidationResult> UpdateAsync(string id, PropertyTraceDto trace)
        {
            var result = await _validator.ValidateAsync(trace);
            if (!result.IsValid) return result;

            var model = trace.ToModel();
            var updateResult = await _traces.ReplaceOneAsync(p => p.Id == id, model);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("Id", "Registro de propiedad no encontrado"));

            return result;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _traces.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
