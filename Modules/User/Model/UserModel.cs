using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.User.Model
{
    public class UserModel
    {
        // ===========================================================
        // 🔹 Identificador (ObjectId en MongoDB)
        // ===========================================================
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;

        // ===========================================================
        // 🔹 Nombre completo
        // ===========================================================
        [BsonElement("Name")]
        [BsonIgnoreIfNull]
        public string Name { get; set; } = string.Empty;

        // ===========================================================
        // 🔹 Correo electrónico
        // ===========================================================
        [BsonElement("Email")]
        [BsonIgnoreIfNull]
        public string Email { get; set; } = string.Empty;

        // ===========================================================
        // 🔹 Contraseña (hash BCrypt)
        // ===========================================================
        [BsonElement("Password")]
        [BsonIgnoreIfNull]
        public string Password { get; set; } = string.Empty;

        // ===========================================================
        // 🔹 Rol del usuario
        // ===========================================================
        [BsonElement("Role")]
        [BsonIgnoreIfNull]
        public string Role { get; set; } = "user";

        // ===========================================================
        // 🔹 Metadatos (opcional para auditoría o trazabilidad)
        // ===========================================================
        [BsonElement("CreatedAt")]
        [BsonIgnoreIfNull]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("UpdatedAt")]
        [BsonIgnoreIfNull]
        public DateTime? UpdatedAt { get; set; }
    }
}
