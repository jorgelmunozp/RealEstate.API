using Microsoft.IdentityModel.Tokens;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.Token.Interface;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.User.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RealEstate.API.Modules.Token.Service
{
    public class JwtService: IJwtService
    {
        private readonly IConfiguration _config;
        private readonly IUserService _userService;

        public JwtService(IConfiguration config, IUserService userService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        // =========================================================
        // Helper: Obtener variable desde entorno o IConfiguration
        // =========================================================
        private string GetEnv(string key, string fallback)
        {
            var fromConfig = _config[key];
            if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig;

            var fromEnv = Environment.GetEnvironmentVariable(key);
            return !string.IsNullOrWhiteSpace(fromEnv) ? fromEnv : fallback;
        }

        // =========================================================
        // Generar JWT genérico
        // =========================================================
        private string GenerateJwtToken(UserModel user, string type, double minutes)
        {
            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));
            var key = Encoding.UTF8.GetBytes(secret);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim("type", type),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================================================
        // Access Token
        // =========================================================
        public string GenerateToken(UserModel user)
        {
            double.TryParse(GetEnv("JwtSettings:ExpiryMinutes", GetEnv("JWT_EXPIRY_MINUTES", "15")), out var minutes);
            return GenerateJwtToken(user, "access", minutes > 0 ? minutes : 15);
        }

        // =========================================================
        // Refresh Token
        // =========================================================
        public string GenerateRefreshToken(UserModel user)
        {
            double.TryParse(GetEnv("JwtSettings:RefreshDays", GetEnv("JWT_REFRESH_DAYS", "7")), out var days);
            return GenerateJwtToken(user, "refresh", (days > 0 ? days : 7) * 24 * 60);
        }

        // =========================================================
        // Generar ambos tokens
        // =========================================================
        public (string AccessToken, string RefreshToken) GenerateTokens(UserModel user)
            => (GenerateToken(user), GenerateRefreshToken(user));

        // =========================================================
        // Validar token
        // =========================================================
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));
            var key = Encoding.UTF8.GetBytes(secret);

            try
            {
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // Lógica de Refresh Token
        // =========================================================
        public async Task<ServiceResultWrapper<object>> ProcessRefreshTokenAsync(string authHeader)
        {
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return ServiceResultWrapper<object>.Fail("Cabecera Authorization inválida o ausente", 401);

            var refreshToken = authHeader["Bearer ".Length..].Trim();
            var principal = ValidateToken(refreshToken);

            if (principal == null)
                return ServiceResultWrapper<object>.Fail("Refresh token inválido o expirado", 401);

            var userId = principal.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid")?.Value;
            if (string.IsNullOrEmpty(userId))
                return ServiceResultWrapper<object>.Fail("No se encontró el ID del usuario en el token", 401);

            var result = await _userService.GetByIdAsync(userId);
            if (!result.Success || result.Data == null)
                return ServiceResultWrapper<object>.Fail("Usuario no encontrado o eliminado", 401);

            var user = result.Data;

            var model = new UserModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Password = user.Password
            };

            var tokens = RefreshAccessToken(refreshToken, model);

            var response = new
            {
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken,
                expiresIn = 60 * 15,
                user
            };

            return ServiceResultWrapper<object>.Ok(response, "Token renovado correctamente");
        }

        // =========================================================
        // Renovar usando refresh token válido
        // =========================================================
        public (string AccessToken, string RefreshToken) RefreshAccessToken(string refreshToken, UserModel user)
        {
            var principal = ValidateToken(refreshToken);
            if (principal == null)
                throw new SecurityTokenException("Refresh token inválido");

            var typeClaim = principal.FindFirst("type")?.Value;
            if (typeClaim != "refresh")
                throw new SecurityTokenException("El token no es de tipo refresh");

            return (GenerateToken(user), GenerateRefreshToken(user));
        }
    }
}
