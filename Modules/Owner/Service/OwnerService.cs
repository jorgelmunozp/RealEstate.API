using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Model;
using RealEstate.API.Modules.Owner.Mapper;

namespace RealEstate.API.Modules.Owner.Service
{
    public class OwnerService
    {
        private readonly IMongoCollection<OwnerModel> _owners;
        private readonly IValidator<OwnerDto> _validator;

        public OwnerService(IMongoDatabase database, IValidator<OwnerDto> validator, IConfiguration config)
        {
            var collection = config["MONGO_COLLECTION_OWNER"]
                        ?? throw new Exception("MONGO_COLLECTION_OWNER no definida");

            _owners = database.GetCollection<OwnerModel>(collection);
            _validator = validator;
        }

        public async Task<List<OwnerDto>> GetAllAsync()
        {
            var owners = await _owners.Find(_ => true).ToListAsync();
            return owners.Select(o => OwnerMapper.ToDto(o)).ToList();
        }

        public async Task<OwnerDto?> GetByIdAsync(string id)
        {
            var owner = await _owners.Find(o => o.Id == id).FirstOrDefaultAsync();
            return owner != null ? OwnerMapper.ToDto(owner) : null;
        }


        public async Task<string> CreateAsync(OwnerDto owner)
        {
            var result = await _validator.ValidateAsync(owner);
            if (!result.IsValid) throw new ValidationException(result.Errors);

            var model = owner.ToModel();
            await _owners.InsertOneAsync(model);
            return model.Id;
        }

        public async Task<ValidationResult> UpdateAsync(string id, OwnerDto owner)
        {
            var result = await _validator.ValidateAsync(owner);
            if (!result.IsValid) return result;
    
            var model = owner.ToModel();
            var updateResult = await _owners.ReplaceOneAsync(o => o.Id == id, model);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("Id", "Propietario no encontrado"));

            return result;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _owners.DeleteOneAsync(o => o.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
