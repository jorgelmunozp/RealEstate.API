using MongoDB.Driver;
using RealEstate.API.Modules.Token.Service;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Service;
using RealEstate.API.Modules.User.Mapper;
using RealEstate.API.Infraestructure.Core.Logs;
using FluentValidation;

namespace RealEstate.API.Modules.Auth.Service
{
    public class AuthService : IAuthService
    {
        private readonly IMongoCollection<UserModel> _userCollection;
        private readonly JwtService _jwtService;
        private readonly IValidator<LoginDto> _validator;
        private readonly UserService _userService;
        private readonly IConfiguration _config;

        public AuthService(IMongoDatabase database, IConfiguration config, JwtService jwtService, IValidator<LoginDto> validator, UserService userService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            var mongoCollectionUser = GetEnv("MONGO_COLLECTION_USER", _config["MONGO_COLLECTION_USER"]);
            if (string.IsNullOrWhiteSpace(mongoCollectionUser))
                throw new InvalidOperationException("MONGO_COLLECTION_USER no configurado");

            _userCollection = database.GetCollection<UserModel>(mongoCollectionUser);
        }

        // =========================================================
        // 🔹 Helper: obtener variable de entorno o IConfiguration
        // =========================================================
        private string? GetEnv(string key, string? fallback = null)
        {
            var fromConfig = _config[key];
            if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig;

            var fromEnv = Environment.GetEnvironmentVariable(key);
            return !string.IsNullOrWhiteSpace(fromEnv) ? fromEnv : fallback;
        }

        // =========================================================
        // 🔹 LOGIN (autenticar usuario)
        // =========================================================
        public async Task<ServiceLogResponseWrapper<object>> LoginAsync(LoginDto loginDto)
        {
            var validationResult = await _validator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return ServiceLogResponseWrapper<object>.Fail("Errores de validación", errors, 400);
            }

            var user = await _userCollection.Find(u => u.Email == loginDto.Email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                return ServiceLogResponseWrapper<object>.Fail("Usuario o contraseña incorrectos", statusCode: 401);

            var tokens = _jwtService.GenerateTokens(user);

            var response = new
            {
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken,
                user = new { user.Id, user.Name, user.Email, user.Role }
            };

            return ServiceLogResponseWrapper<object>.Ok(response, "Inicio de sesión exitoso", 200);
        }

        // =========================================================
        // 🔹 REGISTER (crear usuario nuevo)
        // =========================================================
        public async Task<ServiceLogResponseWrapper<object>> RegisterAsync(UserDto userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.Password))
            {
                return ServiceLogResponseWrapper<object>.Fail(
                    "La contraseña es obligatoria",
                    new List<string> { "Password no puede estar vacío" },
                    400
                );
            }

            var existingUser = await _userService.GetByEmailAsync(userDto.Email);
            if (existingUser != null)
            {
                return ServiceLogResponseWrapper<object>.Fail(
                    "El correo electrónico ya está registrado",
                    new List<string> { "Email duplicado" },
                    400
                );
            }

            var result = await _userService.CreateUserAsync(userDto);
            if (!result.Success)
            {
                return ServiceLogResponseWrapper<object>.Fail(
                    "Errores al crear el usuario",
                    result.Errors ?? new List<string> { "Error desconocido" },
                    400
                );
            }

            var createdUser = result.Data;
            if (createdUser == null)
            {
                return ServiceLogResponseWrapper<object>.Fail(
                    "No se pudo recuperar el usuario creado",
                    new List<string> { "Usuario nulo" },
                    400
                );
            }

            var tokens = _jwtService.GenerateTokens(createdUser.ToModel());

            var response = new
            {
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken,
                user = new
                {
                    createdUser.Id,
                    createdUser.Name,
                    createdUser.Email,
                    createdUser.Role
                }
            };

            return ServiceLogResponseWrapper<object>.Ok(response, "Usuario registrado correctamente", 201);
        }
    }
}
