using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTtl;

        public UserService(IMongoDatabase database, IValidator<UserDto> validator, IConfiguration config, IMemoryCache cache)
        {
            var collection = config["MONGO_COLLECTION_USER"]
                        ?? throw new Exception("MONGO_COLLECTION_USER no definida");

            _users = database.GetCollection<UserModel>(collection);
            _validator = validator;
            _cache = cache;
            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = (int.TryParse(ttlStr, out var m) && m > 0) ? TimeSpan.FromMinutes(m) : TimeSpan.FromMinutes(5);
        }

        // Obtener todos los usuarios
        public async Task<List<UserDto>> GetAllAsync(bool refresh = false)
        {
            var key = "user:all";
            if (!refresh)
            {
                var cached = _cache.Get<List<UserDto>>(key);
                if (cached != null) return cached;
            }
            var users = await _users.Find(_ => true).ToListAsync();
            var result = users.Select(u => u.ToDto()).ToList();
            _cache.Set(key, result, _cacheTtl);
            return result;
        }

        // Obtener usuario por email
        public async Task<UserDto?> GetByEmailAsync(string email, bool refresh = false)
        {
            var key = $"user:email:{email}";
            if (!refresh)
            {
                var cached = _cache.Get<UserDto>(key);
                if (cached != null) return cached;
            }
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            var dto = user?.ToDto();
            if (dto != null) _cache.Set(key, dto, _cacheTtl);
            return dto;
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
