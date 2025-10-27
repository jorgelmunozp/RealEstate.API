using Microsoft.IdentityModel.Tokens;
using RealEstate.API.Infraestructure.Core.Logs;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RealEstate.API.Modules.Token.Service
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly UserService _userService;

        public JwtService(IConfiguration config, UserService userService)
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
        // Generar Access Token (corto)
        // =========================================================
        public string GenerateToken(UserModel user)
        {
            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));
            var expiryMinutesString = GetEnv("JwtSettings:ExpiryMinutes", GetEnv("JWT_EXPIRY_MINUTES", "15"));

            var key = Encoding.UTF8.GetBytes(secret);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            _ = double.TryParse(expiryMinutesString, out var expiryMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim("type", "access"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes > 0 ? expiryMinutes : 15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================================================
        // Generar Refresh Token (largo)
        // =========================================================
        public string GenerateRefreshToken(UserModel user)
        {
            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));
            var refreshDaysString = GetEnv("JwtSettings:RefreshDays", GetEnv("JWT_REFRESH_DAYS", "7"));

            var key = Encoding.UTF8.GetBytes(secret);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            _ = double.TryParse(refreshDaysString, out var refreshDays);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
                new Claim("type", "refresh"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(refreshDays > 0 ? refreshDays : 7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================================================
        // Generar ambos tokens
        // =========================================================
        public (string AccessToken, string RefreshToken) GenerateTokens(UserModel user)
        {
            return (GenerateToken(user), GenerateRefreshToken(user));
        }

        // =========================================================
        // Validar token (acceso o refresh)
        // =========================================================
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));

            var key = Encoding.UTF8.GetBytes(secret);
            var tokenHandler = new JwtSecurityTokenHandler();

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

                return tokenHandler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // Lógica completa del flujo de Refresh
        // =========================================================
        public async Task<ServiceLogResponseWrapper<object>> ProcessRefreshTokenAsync(string authHeader)
        {
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return ServiceLogResponseWrapper<object>.Fail("Cabecera Authorization inválida o ausente", statusCode: 401);

            var refreshToken = authHeader["Bearer ".Length..].Trim();
            var principal = ValidateToken(refreshToken);

            if (principal == null)
                return ServiceLogResponseWrapper<object>.Fail("Refresh token inválido o expirado", statusCode: 401);

            var userId = principal.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid")?.Value;
            if (string.IsNullOrEmpty(userId))
                return ServiceLogResponseWrapper<object>.Fail("No se encontró el ID del usuario en el token", statusCode: 401);

            var userDto = await _userService.GetByIdAsync(userId);
            if (userDto == null)
                return ServiceLogResponseWrapper<object>.Fail("Usuario no encontrado o eliminado", statusCode: 401);

            var userModel = new UserModel
            {
                Id = userDto.Id,
                Name = userDto.Name,
                Email = userDto.Email,
                Role = userDto.Role,
                Password = userDto.Password
            };

            var tokens = RefreshAccessToken(refreshToken, userModel);

            var response = new
            {
                accessToken = tokens.AccessToken,
                refreshToken = tokens.RefreshToken,
                expiresIn = 60 * 15,
                user = userDto
            };

            return ServiceLogResponseWrapper<object>.Ok(response, "Token renovado correctamente", 200);
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

            var newAccessToken = GenerateToken(user);
            var newRefreshToken = GenerateRefreshToken(user);

            return (newAccessToken, newRefreshToken);
        }
    }
}
