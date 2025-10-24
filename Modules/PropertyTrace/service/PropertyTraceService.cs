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

        // 🔹 Obtener todas las trazas
        public async Task<List<PropertyTraceDto>> GetAllAsync(string? idProperty = null)
        {
            var filter = Builders<PropertyTraceModel>.Filter.Empty;

            if (!string.IsNullOrEmpty(idProperty))
                filter &= Builders<PropertyTraceModel>.Filter.Eq(t => t.IdProperty, idProperty);

            var traces = await _traces.Find(filter).ToListAsync();
            return traces.Select(PropertyTraceMapper.ToDto).ToList();
        }

        // 🔹 Obtener una traza por ID
        public async Task<PropertyTraceDto?> GetByIdAsync(string id)
        {
            var trace = await _traces.Find(p => p.Id == id).FirstOrDefaultAsync();
            return trace != null ? PropertyTraceMapper.ToDto(trace) : null;
        }

        // 🔹 Crear una o varias trazas
        public async Task<List<string>> CreateAsync(IEnumerable<PropertyTraceDto> traces)
        {
            var ids = new List<string>();
            var allErrors = new List<ValidationFailure>();

            foreach (var trace in traces)
            {
                var result = await _validator.ValidateAsync(trace);
                if (!result.IsValid)
                {
                    allErrors.AddRange(result.Errors);
                    continue;
                }

                var model = trace.ToModel();
                await _traces.InsertOneAsync(model);
                ids.Add(model.Id);
            }

            if (allErrors.Any())
                throw new ValidationException(allErrors);

            return ids;
        }

        // 🔹 Reemplazo completo (PUT)
        public async Task<ValidationResult> UpdateAsync(string id, PropertyTraceDto trace)
        {
            var result = await _validator.ValidateAsync(trace);
            if (!result.IsValid) return result;

            var existing = await _traces.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("Id", "Registro de propiedad no encontrado"));
                return result;
            }

            var model = trace.ToModel();
            model.Id = id; // conservar ID original

            await _traces.ReplaceOneAsync(p => p.Id == id, model);
            return result;
        }

        // 🔹 Actualización parcial (PATCH)
        public async Task<ValidationResult> UpdatePartialAsync(string id, PropertyTraceDto trace)
        {
            var existing = await _traces.Find(p => p.Id == id).FirstOrDefaultAsync();
            var result = await _validator.ValidateAsync(trace);

            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("Id", "Registro no encontrado"));
                return result;
            }

            // Solo reemplazar campos no nulos ni vacíos
            if (!string.IsNullOrEmpty(trace.Name)) existing.Name = trace.Name;
            if (trace.Value != 0) existing.Value = trace.Value;
            if (trace.Tax != 0) existing.Tax = trace.Tax;
            if (!string.IsNullOrEmpty(trace.DateSale)) existing.DateSale = trace.DateSale;
            if (!string.IsNullOrEmpty(trace.IdProperty)) existing.IdProperty = trace.IdProperty;

            await _traces.ReplaceOneAsync(p => p.Id == id, existing);
            return result;
        }

        // 🔹 Eliminar una traza
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _traces.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
