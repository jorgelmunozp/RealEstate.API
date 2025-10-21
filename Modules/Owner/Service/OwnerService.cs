using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.Owner.Dto;

namespace RealEstate.API.Modules.Owner.Service
{
    public class OwnerService
    {
        private readonly IMongoCollection<OwnerDto> _owners;
        private readonly IValidator<OwnerDto> _validator;

        public OwnerService(IMongoDatabase database, IValidator<OwnerDto> validator, IConfiguration config)
        {
            var collection = config["MONGO_COLLECTION_OWNER"]
                        ?? throw new Exception("MONGO_COLLECTION_OWNER no definida");

            _owners = database.GetCollection<OwnerDto>(collection);
            _validator = validator;
        }

        public async Task<List<OwnerDto>> GetAllAsync() =>
            await _owners.Find(_ => true).ToListAsync();

        public async Task<OwnerDto?> GetByIdAsync(string id) =>
            await _owners.Find(o => o.IdOwner == id).FirstOrDefaultAsync();

        public async Task<ValidationResult> CreateAsync(OwnerDto owner)
        {
            var result = await _validator.ValidateAsync(owner);
            if (!result.IsValid) return result;

            await _owners.InsertOneAsync(owner);
            return result;
        }

        public async Task<ValidationResult> UpdateAsync(string id, OwnerDto owner)
        {
            var result = await _validator.ValidateAsync(owner);
            if (!result.IsValid) return result;

            var updateResult = await _owners.ReplaceOneAsync(o => o.IdOwner == id, owner);
            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("IdOwner", "Propietario no encontrado"));

            return result;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _owners.DeleteOneAsync(o => o.IdOwner == id);
            return result.DeletedCount > 0;
        }
    }
}
