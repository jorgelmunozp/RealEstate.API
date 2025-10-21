using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.User.Mapper;

namespace RealEstate.API.Modules.User.Service
{
    public class UserService
    {
        private readonly IMongoCollection<UserModel> _users;
        private readonly IValidator<UserDto> _validator;

        public UserService(IMongoDatabase database, IValidator<UserDto> validator, IConfiguration config)
        {
            var collection = config["MONGO_COLLECTION_USER"]
                        ?? throw new Exception("MONGO_COLLECTION_USER no definida");

            _users = database.GetCollection<UserModel>(collection);
            _validator = validator;
        }

        // Obtener todos los usuarios
        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = await _users.Find(_ => true).ToListAsync();
            return users.Select(u => u.ToDto()).ToList();
        }

        // Obtener usuario por email
        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            return user?.ToDto();
        }

        // Crear nuevo usuario
        public async Task<ValidationResult> CreateAsync(UserDto user)
        {
            var result = await _validator.ValidateAsync(user);
            if (!result.IsValid) return result;

            var model = user.ToModel();
            await _users.InsertOneAsync(model);
            return result;
        }

        // Actualizar usuario
        public async Task<ValidationResult> UpdateAsync(string email, UserDto user)
        {
            var result = await _validator.ValidateAsync(user);
            if (!result.IsValid) return result;

            var model = user.ToModel();
            var updateResult = await _users.ReplaceOneAsync(u => u.Email == email, model);

            if (updateResult.MatchedCount == 0)
                result.Errors.Add(new ValidationFailure("Email", "Usuario no encontrado"));

            return result;
        }

        // Eliminar usuario
        public async Task<bool> DeleteAsync(string email)
        {
            var result = await _users.DeleteOneAsync(u => u.Email == email);
            return result.DeletedCount > 0;
        }
    }
}
