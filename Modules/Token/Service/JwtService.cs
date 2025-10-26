using RealEstate.API.Modules.User.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RealEstate.API.Modules.Token.Service
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // =========================================================
        // 🔹 MÉTODOS AUXILIARES PARA VARIABLES DE ENTORNO
        // =========================================================
        private string GetEnv(string key, string fallback)
        {
            // Prioriza IConfiguration (Program.cs o appsettings.json)
            var fromConfig = _config[key];
            if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig;

            // Si no existe, intenta leer desde variables de entorno (.env)
            var fromEnv = Environment.GetEnvironmentVariable(key);
            return !string.IsNullOrWhiteSpace(fromEnv) ? fromEnv : fallback;
        }

        // =========================================================
        // 🔹 GENERAR TOKEN DE ACCESO (CORTA DURACIÓN)
        // =========================================================
        public string GenerateToken(UserModel user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT_SECRET no configurada");

            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));
            var expiryMinutesString = GetEnv("JwtSettings:ExpiryMinutes", GetEnv("JWT_EXPIRY_MINUTES", "15"));

            var key = Encoding.UTF8.GetBytes(secret);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            if (!double.TryParse(expiryMinutesString, out var expiryMinutes)) expiryMinutes = 15;

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
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================================================
        // 🔹 GENERAR REFRESH TOKEN (LARGA DURACIÓN)
        // =========================================================
        public string GenerateRefreshToken(UserModel user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT_SECRET no configurada");

            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));
            var refreshDaysString = GetEnv("JwtSettings:RefreshDays", GetEnv("JWT_REFRESH_DAYS", "7"));

            var key = Encoding.UTF8.GetBytes(secret);
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            if (!double.TryParse(refreshDaysString, out var refreshDays)) refreshDays = 7;

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
                expires: DateTime.UtcNow.AddDays(refreshDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================================================
        // 🔹 GENERAR AMBOS TOKENS (LOGIN / RENOVACIÓN)
        // =========================================================
        public (string AccessToken, string RefreshToken) GenerateTokens(UserModel user)
        {
            var accessToken = GenerateToken(user);
            var refreshToken = GenerateRefreshToken(user);
            return (accessToken, refreshToken);
        }

        // =========================================================
        // 🔹 VALIDAR TOKEN (ACCESO O REFRESH)
        // =========================================================
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET", ""));
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT_SECRET no configurada");

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
        // 🔹 RENOVAR TOKEN DE ACCESO USANDO REFRESH TOKEN
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
