using MongoDB.Driver;
using RealEstate.API.Modules.Token.Service;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Service;
using FluentValidation;
using FluentValidation.Results;

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
        // 🔹 Helper: Obtener variable desde entorno o IConfiguration
        // =========================================================
        private string? GetEnv(string key, string? fallback = null)
        {
            var fromConfig = _config[key];
            if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig;

            var fromEnv = Environment.GetEnvironmentVariable(key);
            return !string.IsNullOrWhiteSpace(fromEnv) ? fromEnv : fallback;
        }

        // =========================================================
        // 🔹 AUTENTICAR USUARIO
        // =========================================================
        public async Task<string> LoginAsync(LoginDto loginDto)
        {
            // 1️⃣ Validar DTO
            var validationResult = await _validator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // 2️⃣ Buscar usuario por correo
            var user = await _userCollection.Find(u => u.Email == loginDto.Email).FirstOrDefaultAsync();
            if (user == null)
                throw new InvalidOperationException("Usuario o contraseña incorrectos");

            // 3️⃣ Verificar contraseña (BCrypt)
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                throw new InvalidOperationException("Usuario o contraseña incorrectos");

            // 4️⃣ Generar tokens (Access + Refresh)
            var tokens = _jwtService.GenerateTokens(user);
            return tokens.AccessToken;
        }

        // =========================================================
        // 🔹 REGISTRAR NUEVO USUARIO
        // =========================================================
        public async Task<ValidationResult> RegisterAsync(UserDto userDto)
        {
            // 1️⃣ Validación mínima
            var prelim = new ValidationResult();
            if (string.IsNullOrWhiteSpace(userDto.Password))
            {
                prelim.Errors.Add(new ValidationFailure("Password", "La contraseña es obligatoria"));
                return prelim;
            }

            // 2️⃣ Verificar si el correo ya está registrado
            var existingUser = await _userService.GetByEmailAsync(userDto.Email);
            if (existingUser != null)
            {
                var validation = new ValidationResult();
                validation.Errors.Add(new ValidationFailure("Email", "El email ya está registrado"));
                return validation;
            }

            // 3️⃣ Crear usuario (UserService aplica hash automáticamente)
            var result = await _userService.CreateAsync(userDto);
            return result;
        }
    }
}
