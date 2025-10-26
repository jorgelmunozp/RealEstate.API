using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.User.Mapper;
using MongoDB.Bson;
using System.Reflection;

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

            // Hash de contraseña antes de persistir
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            }

            var model = user.ToModel();
            await _users.InsertOneAsync(model);

            // Invalida cache relevante
            _cache.Remove("user:all");
            _cache.Remove($"user:email:{user.Email}");
            return result;
        }

        // Actualizar usuario
        public async Task<ValidationResult> UpdateAsync(string email, UserDto user)
        {
            var result = await _validator.ValidateAsync(user);
            if (!result.IsValid) return result;

            var existing = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existing == null)
            {
                result.Errors.Add(new ValidationFailure("Email", "Usuario no encontrado"));
                return result;
            }

            // Verificar unicidad si cambia el email
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _users.Find(u => u.Email == user.Email).AnyAsync();
                if (emailTaken)
                {
                    result.Errors.Add(new ValidationFailure("Email", "El email ya está registrado"));
                    return result;
                }
            }

            // Hash de contraseña si viene informada
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            }

            var model = user.ToModel();
            model.Id = existing.Id; // preservar Id

            var updateResult = await _users.ReplaceOneAsync(u => u.Id == existing.Id, model);

            // Invalida cache relevante
            _cache.Remove("user:all");
            _cache.Remove($"user:email:{email}");
            _cache.Remove($"user:email:{user.Email}");

            return result;
        }

        // Eliminar usuario
        public async Task<bool> DeleteAsync(string email)
        {
            var result = await _users.DeleteOneAsync(u => u.Email == email);
            var ok = result.DeletedCount > 0;
            if (ok)
            {
                _cache.Remove("user:all");
                _cache.Remove($"user:email:{email}");
            }
            return ok;
        }

        // PATCH parcial (solo campos enviados)
        public async Task<UserDto?> PatchAsync(string email, Dictionary<string, object> fields)
        {
            if (fields == null || fields.Count == 0) return null;

            var existing = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existing == null) return null;

            var updates = new List<UpdateDefinition<UserModel>>();
            var builder = Builders<UserModel>.Update;

            // Lista blanca de campos permitidos
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Name", "Email", "Password", "Role" };

            string? newEmail = null;

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Key)) continue;
                if (!allowed.Contains(field.Key)) continue; // ignora campos no permitidos (ej: Id)

                var keyNorm = typeof(UserModel).GetProperty(field.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.Name;
                if (string.IsNullOrEmpty(keyNorm)) continue;

                if (string.Equals(keyNorm, nameof(UserModel.Password), StringComparison.Ordinal))
                {
                    if (field.Value is string pwd && !string.IsNullOrWhiteSpace(pwd))
                    {
                        var hash = BCrypt.Net.BCrypt.HashPassword(pwd);
                        updates.Add(builder.Set(nameof(UserModel.Password), BsonValue.Create(hash)));
                    }
                    continue;
                }

                if (string.Equals(keyNorm, nameof(UserModel.Email), StringComparison.Ordinal))
                {
                    if (field.Value is string emailNew && !string.IsNullOrWhiteSpace(emailNew))
                    {
                        newEmail = emailNew;
                        // Unicidad del email
                        if (!string.Equals(existing.Email, emailNew, StringComparison.OrdinalIgnoreCase))
                        {
                            var taken = await _users.Find(u => u.Email == emailNew).AnyAsync();
                            if (taken)
                            {
                                throw new FluentValidation.ValidationException(new[] {
                                    new FluentValidation.Results.ValidationFailure("Email", "El email ya está registrado")
                                });
                            }
                        }
                        updates.Add(builder.Set(nameof(UserModel.Email), BsonValue.Create(emailNew)));
                    }
                    continue;
                }

                updates.Add(builder.Set(keyNorm, BsonValue.Create(field.Value)));
            }

            if (!updates.Any()) return null;

            var update = builder.Combine(updates);
            await _users.UpdateOneAsync(u => u.Id == existing.Id, update);

            // Invalida cache relevante
            _cache.Remove("user:all");
            _cache.Remove($"user:email:{existing.Email}");
            if (!string.IsNullOrWhiteSpace(newEmail))
                _cache.Remove($"user:email:{newEmail}");

            var updated = await _users.Find(u => u.Id == existing.Id).FirstOrDefaultAsync();
            return updated?.ToDto();
        }
    }
}
