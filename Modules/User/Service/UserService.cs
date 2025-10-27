using FluentValidation;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using AutoMapper;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Infraestructure.Core.Services;

namespace RealEstate.API.Modules.User.Service
{
    public class UserService
    {
        private readonly IMongoCollection<UserModel> _users;
        private readonly IValidator<UserDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly TimeSpan _cacheTtl;

        public UserService(IMongoDatabase database, IValidator<UserDto> validator, IConfiguration config, IMemoryCache cache, IMapper mapper)
        {
            var collection = config["MONGO_COLLECTION_USER"]
                ?? throw new InvalidOperationException("MONGO_COLLECTION_USER no definida");

            _users = database.GetCollection<UserModel>(collection);
            _validator = validator;
            _cache = cache;
            _mapper = mapper;

            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = (int.TryParse(ttlStr, out var minutes) && minutes > 0)
                ? TimeSpan.FromMinutes(minutes)
                : TimeSpan.FromMinutes(5);
        }

        // ===========================================================
        // 🔹 GET ALL
        // ===========================================================
        public async Task<List<UserDto>> GetAllAsync(bool refresh = false)
        {
            const string key = "user:all";
            if (!refresh && _cache.TryGetValue(key, out List<UserDto>? cached))
                return cached!;

            var users = await _users.Find(_ => true).ToListAsync();
            var result = _mapper.Map<List<UserDto>>(users);
            _cache.Set(key, result, _cacheTtl);
            return result;
        }

        // ===========================================================
        // 🔹 GET BY EMAIL
        // ===========================================================
        public async Task<UserDto?> GetByEmailAsync(string email, bool refresh = false)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var key = $"user:email:{email}";
            if (!refresh && _cache.TryGetValue(key, out UserDto? cached))
                return cached!;

            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            var dto = _mapper.Map<UserDto>(user);
            if (dto != null) _cache.Set(key, dto, _cacheTtl);
            return dto;
        }

        // ===========================================================
        // 🔹 GET BY ID
        // ===========================================================
        public async Task<UserDto?> GetByIdAsync(string id, bool refresh = false)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var key = $"user:id:{id}";
            if (!refresh && _cache.TryGetValue(key, out UserDto? cached))
                return cached!;

            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            var dto = _mapper.Map<UserDto>(user);
            if (dto != null) _cache.Set(key, dto, _cacheTtl);
            return dto;
        }

        // ===========================================================
        // 🔹 CREATE USER
        // ===========================================================
        public async Task<ServiceResultWrapper<UserDto>> CreateUserAsync(UserDto user)
        {
            var validation = await _validator.ValidateAsync(user);
            if (!validation.IsValid)
                return ServiceResultWrapper<UserDto>.Fail(validation.Errors.Select(e => e.ErrorMessage));

            var exists = await _users.Find(u => u.Email == user.Email).AnyAsync();
            if (exists)
                return ServiceResultWrapper<UserDto>.Fail("El email ya está registrado");

            if (!string.IsNullOrWhiteSpace(user.Password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            var model = _mapper.Map<UserModel>(user);
            await _users.InsertOneAsync(model);

            _cache.Remove("user:all");
            _cache.Remove($"user:email:{user.Email}");

            return ServiceResultWrapper<UserDto>.Created(
                _mapper.Map<UserDto>(model),
                "Usuario creado correctamente"
            );
        }

        // ===========================================================
        // 🔹 UPDATE USER
        // ===========================================================
        public async Task<ServiceResultWrapper<UserDto>> UpdateUserAsync(string email, UserDto user, string requesterRole)
        {
            var existing = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<UserDto>.Fail("Usuario no encontrado", 404);

            bool isAdmin = string.Equals(requesterRole, "admin", StringComparison.OrdinalIgnoreCase);
            bool isChangingRole = !string.Equals(existing.Role, user.Role, StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && isChangingRole)
                return ServiceResultWrapper<UserDto>.Fail("Solo un administrador puede cambiar el rol", 403);

            if (!isAdmin)
                user.Role = existing.Role;

            var validation = await _validator.ValidateAsync(user);
            if (!validation.IsValid)
                return ServiceResultWrapper<UserDto>.Fail(validation.Errors.Select(e => e.ErrorMessage));

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _users.Find(u => u.Email == user.Email).AnyAsync();
                if (emailTaken)
                    return ServiceResultWrapper<UserDto>.Fail("El correo electrónico ya está en uso");
            }

            if (!string.IsNullOrWhiteSpace(user.Password))
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            var updatedModel = _mapper.Map(user, existing);
            await _users.ReplaceOneAsync(u => u.Id == existing.Id, updatedModel);

            _cache.Remove("user:all");
            _cache.Remove($"user:email:{email}");
            _cache.Remove($"user:email:{user.Email}");

            return ServiceResultWrapper<UserDto>.Updated(
                _mapper.Map<UserDto>(updatedModel),
                "Usuario actualizado correctamente"
            );
        }

        // ===========================================================
        // 🔹 PATCH USER (seguro y compatible con JsonElement)
        // ===========================================================
        public async Task<ServiceResultWrapper<UserDto>> PatchUserAsync(string email, Dictionary<string, object> fields, string requesterRole)
        {
            if (fields == null || fields.Count == 0)
                return ServiceResultWrapper<UserDto>.Fail("No se enviaron campos para actualizar");

            var existing = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existing == null)
                return ServiceResultWrapper<UserDto>.Fail("Usuario no encontrado", 404);

            bool isAdmin = string.Equals(requesterRole, "admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && fields.Keys.Any(k => string.Equals(k, "role", StringComparison.OrdinalIgnoreCase)))
                return ServiceResultWrapper<UserDto>.Fail("Solo un administrador puede cambiar el rol", 403);

            var builder = Builders<UserModel>.Update;
            var updates = new List<UpdateDefinition<UserModel>>();
            string? newEmail = null;

            foreach (var field in fields)
            {
                var prop = typeof(UserModel).GetProperty(
                    field.Key,
                    System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                );

                if (prop == null) continue;

                // ✅ Normaliza el valor (manejo seguro de JsonElement)
                object? value = field.Value;
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    switch (jsonElement.ValueKind)
                    {
                        case System.Text.Json.JsonValueKind.String:
                            value = jsonElement.GetString();
                            break;
                        case System.Text.Json.JsonValueKind.Number:
                            if (jsonElement.TryGetInt64(out long longVal))
                                value = longVal;
                            else if (jsonElement.TryGetDouble(out double doubleVal))
                                value = doubleVal;
                            break;
                        case System.Text.Json.JsonValueKind.True:
                        case System.Text.Json.JsonValueKind.False:
                            value = jsonElement.GetBoolean();
                            break;
                        case System.Text.Json.JsonValueKind.Null:
                        case System.Text.Json.JsonValueKind.Undefined:
                            value = null;
                            break;
                    }
                }

                switch (prop.Name)
                {
                    case nameof(UserModel.Password):
                        if (value is string pwd && !string.IsNullOrWhiteSpace(pwd))
                            updates.Add(builder.Set(prop.Name, BCrypt.Net.BCrypt.HashPassword(pwd)));
                        break;

                    case nameof(UserModel.Email):
                        if (value is string newMail && !string.IsNullOrWhiteSpace(newMail))
                        {
                            newEmail = newMail;
                            if (!string.Equals(existing.Email, newMail, StringComparison.OrdinalIgnoreCase))
                            {
                                var taken = await _users.Find(u => u.Email == newMail).AnyAsync();
                                if (taken)
                                    return ServiceResultWrapper<UserDto>.Fail("El email ya está registrado");
                            }
                            updates.Add(builder.Set(prop.Name, newMail));
                        }
                        break;

                    case nameof(UserModel.Role):
                        if (isAdmin && value is string newRole && !string.IsNullOrWhiteSpace(newRole))
                            updates.Add(builder.Set(prop.Name, newRole));
                        break;

                    default:
                        updates.Add(builder.Set(prop.Name, BsonValue.Create(value)));
                        break;
                }
            }

            if (!updates.Any())
                return ServiceResultWrapper<UserDto>.Fail("Sin cambios válidos para aplicar");

            await _users.UpdateOneAsync(u => u.Id == existing.Id, builder.Combine(updates));

            _cache.Remove("user:all");
            _cache.Remove($"user:email:{existing.Email}");
            if (!string.IsNullOrWhiteSpace(newEmail))
                _cache.Remove($"user:email:{newEmail}");

            var updated = await _users.Find(u => u.Id == existing.Id).FirstOrDefaultAsync();
            return ServiceResultWrapper<UserDto>.Updated(
                _mapper.Map<UserDto>(updated),
                "Usuario actualizado parcialmente"
            );
        }

        // ===========================================================
        // 🔹 DELETE USER
        // ===========================================================
        public async Task<ServiceResultWrapper<bool>> DeleteUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResultWrapper<bool>.Fail("Email inválido");

            var result = await _users.DeleteOneAsync(u => u.Email == email);
            if (result.DeletedCount == 0)
                return ServiceResultWrapper<bool>.Fail("Usuario no encontrado", 404);

            _cache.Remove("user:all");
            _cache.Remove($"user:email:{email}");

            return ServiceResultWrapper<bool>.Deleted("Usuario eliminado correctamente");
        }
    }
}
