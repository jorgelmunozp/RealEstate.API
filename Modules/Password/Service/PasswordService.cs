using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using RealEstate.API.Modules.User.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace RealEstate.API.Modules.Password.Service
{
    public class PasswordService
    {
        private readonly IMongoCollection<UserModel> _users;
        private readonly IConfiguration _config;

        public PasswordService(IMongoDatabase database, IConfiguration config)
        {
            _config = config;
            var collection = config["MONGO_COLLECTION_USER"]
                            ?? throw new Exception("MONGO_COLLECTION_USER no definida");
            _users = database.GetCollection<UserModel>(collection);
        }

        public async Task<object> SendPasswordRecoveryEmail(string email)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null) throw new InvalidOperationException($"No existe usuario con el email {email}");

            var token = GenerateResetToken(user.Id);

            var frontendUrl = _config["FRONTEND_URL"];
            var resetLink = $"{frontendUrl.TrimEnd('/')}/password-reset/{token}";

            var smtpHost = _config["SMTP_HOST"];
            var smtpPortStr = _config["SMTP_PORT"] ?? "587";
            var smtpUser = _config["SMTP_USER"];
            var smtpPass = _config["SMTP_PASS"];

            if (!int.TryParse(smtpPortStr, out var smtpPort)) smtpPort = 587;

            if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = smtpPort == 465 || smtpPort == 587,
                    Credentials = new NetworkCredential(smtpUser, smtpPass)
                };

                var msg = new MailMessage
                {
                    From = new MailAddress(smtpUser, "Soporte"),
                    Subject = "Real Estate - Recuperación de contraseña",
                    Body = $@"<h2>Hola {WebUtility.HtmlEncode(user.Name)}</h2>
<p>Has solicitado restablecer tu contraseña de Real Estate</p>
<p>Haz clic en el siguiente enlace para establecer una nueva contraseña (válido por 15 minutos):</p>
<a href=""{resetLink}"">Restablecer contraseña</a>
<p style=""color:gray;font-size:12px;"">Este correo fue generado automáticamente, no respondas a este mensaje.</p>",
                    IsBodyHtml = true
                };
                msg.To.Add(email);

                await client.SendMailAsync(msg);
            }

            return new { message = $"Enlace de recuperación enviado al correo {email}" };
        }

        public object VerifyResetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Token requerido");

            var principal = ValidateToken(token);
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Token inválido");
            return new { message = "Valid token", id };
        }

        public async Task<object> UpdatePasswordById(string id, string newPassword)
        {
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null) throw new InvalidOperationException("Usuario no encontrado");

            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var update = Builders<UserModel>.Update.Set(u => u.Password, hashed);
            await _users.UpdateOneAsync(u => u.Id == id, update);
            return new { message = "Contraseña actualizada exitosamente" };
        }

        private string GenerateResetToken(string id)
        {
            var secret = _config["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey no configurada");
            var issuer = _config["JwtSettings:Issuer"] ?? "RealEstateAPI";
            var audience = _config["JwtSettings:Audience"] ?? "UsuariosAPI";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id),
                new Claim(ClaimTypes.NameIdentifier, id)
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal ValidateToken(string token)
        {
            var secret = _config["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey no configurada");
            var issuer = _config["JwtSettings:Issuer"] ?? "RealEstateAPI";
            var audience = _config["JwtSettings:Audience"] ?? "UsuariosAPI";

            var tokenHandler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            return tokenHandler.ValidateToken(token, parameters, out _);
        }
    }
}
