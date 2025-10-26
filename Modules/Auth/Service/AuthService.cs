using MongoDB.Driver;
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


        public AuthService(IMongoDatabase database, IConfiguration config, JwtService jwtService, IValidator<LoginDto> validator, UserService userService)
        {
            _jwtService = jwtService;
            _validator = validator;
            _userService = userService;
            var mongoCollectionUser = config["MONGO_COLLECTION_USER"] 
                                      ?? throw new InvalidOperationException("MONGO_COLLECTION_USER no configurado");
            _userCollection = database.GetCollection<UserModel>(mongoCollectionUser);
        }

        //********* AUTENTICAR USUARIO *********//
        public async Task<string> LoginAsync(LoginDto loginDto)
        {
            // Valida DTO
            var validationResult = await _validator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var user = await _userCollection.Find(u => u.Email == loginDto.Email).FirstOrDefaultAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Usuario o contraseña incorrectos");
            }

            // Verificar contrase�a (BCrypt)
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                throw new InvalidOperationException("Usuario o contrase�a incorrectos");
            }

            // 4️⃣ Generar JWT
            var token = _jwtService.GenerateToken(user);
            return token;
        }


        //********* REGISTRAR USUARIO *********//
        public async Task<ValidationResult> RegisterAsync(UserDto userDto)
        {
            // Reglas mínimas de seguridad adicionales
            var prelim = new ValidationResult();
            if (string.IsNullOrWhiteSpace(userDto.Password))
            {
                prelim.Errors.Add(new FluentValidation.Results.ValidationFailure("Password", "La contraseña es obligatoria"));
                return prelim;
            }
            // Valida si el usuario ya existe
            var existingUser = await _userService.GetByEmailAsync(userDto.Email);
            if (existingUser != null)
            {
                var validation = new ValidationResult();
                validation.Errors.Add(new FluentValidation.Results.ValidationFailure("Email", "El email ya está registrado"));
                return validation;
            }

            // Crear usuario usando UserService (hash se aplica en UserService)
            var result = await _userService.CreateAsync(userDto);

            return result;
        }
    }
}

