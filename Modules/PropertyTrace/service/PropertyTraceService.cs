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

        public PropertyTraceService(
            IMongoDatabase database,
            IValidator<PropertyTraceDto> validator,
            IConfiguration config)
        {
            var collection = config["MONGO_COLLECTION_PROPERTYTRACE"] 
                             ?? throw new Exception("MONGO_COLLECTION_PROPERTYTRACE no definida");
            _traces = database.GetCollection<PropertyTraceModel>(collection);
            _validator = validator;
        }

        // Devuelve todos los registros como lista
        public async Task<List<PropertyTraceDto>> GetAllAsync(string? idProperty = null)
        {
            var filter = Builders<PropertyTraceModel>.Filter.Empty;

            if (!string.IsNullOrEmpty(idProperty))
                filter &= Builders<PropertyTraceModel>.Filter.Eq(t => t.IdProperty, idProperty);

            var traces = await _traces.Find(filter).ToListAsync();

            return traces.Select(PropertyTraceMapper.ToDto).ToList();
        }


        // Devuelve un único registro, puede ser null si no existe
        // El controller se encarga de envolverlo en lista
        public async Task<PropertyTraceDto?> GetByIdAsync(string id)
        {
            var trace = await _traces.Find(p => p.Id == id).FirstOrDefaultAsync();
            return trace != null ? PropertyTraceMapper.ToDto(trace) : null;
        }

        // Crea múltiples registros a la vez
        public async Task<List<string>> CreateAsync(IEnumerable<PropertyTraceDto> traces)
        {
            var ids = new List<string>();
            var allErrors = new List<ValidationFailure>();

            foreach (var trace in traces)
            {
                // Validación de cada DTO
                var result = await _validator.ValidateAsync(trace);
                if (!result.IsValid)
                {
                    allErrors.AddRange(result.Errors);
                    continue; // no insertar DTO inválido
                }

                // Convertir a modelo y guardar en MongoDB
                var model = trace.ToModel();
                await _traces.InsertOneAsync(model);
                ids.Add(model.Id);
            }

            // Lanzar excepción si hubo errores de validación
            if (allErrors.Any())
                throw new ValidationException(allErrors);

            return ids;
        }

        // Actualiza un registro existente
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

        // Elimina un registro por Id
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _traces.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
