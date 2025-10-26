using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using RealEstate.API.Modules.User.Model;
using RealEstate.API.Modules.Token.Service;
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
        private readonly JwtService _jwtService;

        public PasswordService(IMongoDatabase database, IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var collection = GetEnv("MONGO_COLLECTION_USER", _config["MONGO_COLLECTION_USER"]);
            if (string.IsNullOrWhiteSpace(collection))
                throw new Exception("MONGO_COLLECTION_USER no definida");

            _users = database.GetCollection<UserModel>(collection);
            _jwtService = new JwtService(config);
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
        // 🔹 Enviar correo de recuperación
        // =========================================================
        public async Task<object> SendPasswordRecoveryEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo electrónico es requerido.");

            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null)
                throw new InvalidOperationException($"No existe usuario con el email {email}");

            // ✅ Generar token de recuperación usando JwtService
            var token = GenerateResetToken(user.Id);

            var frontendUrl = GetEnv("FRONTEND_URL", "http://localhost:3000");
            var baseUrl = frontendUrl.TrimEnd('/');
            var resetLink = $"{baseUrl}/password-reset/{token}";

            // 📧 SMTP settings
            var smtpHost = GetEnv("SMTP_HOST", "smtp.gmail.com");
            var smtpPortStr = GetEnv("SMTP_PORT", "587");
            var smtpUser = GetEnv("SMTP_USER");
            var smtpPass = GetEnv("SMTP_PASS");

            if (!int.TryParse(smtpPortStr, out var smtpPort)) smtpPort = 587;

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
                throw new InvalidOperationException("Configuración SMTP incompleta.");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = smtpPort == 465 || smtpPort == 587,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var msg = new MailMessage
            {
                From = new MailAddress(smtpUser, "Soporte RealEstate"),
                Subject = "Real Estate - Recuperación de contraseña",
                Body = $@"
                    <h2>Hola {WebUtility.HtmlEncode(user.Name)}</h2>
                    <p>Has solicitado restablecer tu contraseña.</p>
                    <p>Haz clic en el siguiente enlace para establecer una nueva contraseña (válido por 15 minutos):</p>
                    <p><a href=""{resetLink}"" target=""_blank"">Restablecer contraseña</a></p>
                    <p style=""color:gray;font-size:12px;"">
                        Este correo fue generado automáticamente, no respondas a este mensaje.
                    </p>",
                IsBodyHtml = true
            };
            msg.To.Add(email);

            await client.SendMailAsync(msg);

            return new { message = $"Enlace de recuperación enviado al correo {email}" };
        }

        // =========================================================
        // 🔹 Verificar token de recuperación
        // =========================================================
        public object VerifyResetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Token requerido.");

            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
                throw new InvalidOperationException("Token inválido o expirado.");

            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Token inválido.");

            return new { message = "Token válido", id };
        }

        // =========================================================
        // 🔹 Actualizar contraseña por ID de usuario
        // =========================================================
        public async Task<object> UpdatePasswordById(string id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El ID de usuario es requerido.");
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("La nueva contraseña es requerida.");

            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                throw new InvalidOperationException("Usuario no encontrado.");

            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var update = Builders<UserModel>.Update.Set(u => u.Password, hashed);
            await _users.UpdateOneAsync(u => u.Id == id, update);

            return new { message = "Contraseña actualizada exitosamente." };
        }

        // =========================================================
        // 🔹 Generar token de recuperación (15 min)
        // =========================================================
        private string GenerateResetToken(string id)
        {
            var secret = GetEnv("JwtSettings:SecretKey", GetEnv("JWT_SECRET"))
                ?? throw new InvalidOperationException("JWT_SECRET no configurada.");

            var issuer = GetEnv("JwtSettings:Issuer", GetEnv("JWT_ISSUER", "RealEstateAPI"));
            var audience = GetEnv("JwtSettings:Audience", GetEnv("JWT_AUDIENCE", "UsuariosAPI"));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id),
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim("type", "password-reset")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
