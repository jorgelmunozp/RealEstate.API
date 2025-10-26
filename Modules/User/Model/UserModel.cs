using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.User.Model
{
    public class UserModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        [BsonElement("Id")]
        public string Id { get; set; } = string.Empty;

        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("Password")]
        public string Password { get; set; } = string.Empty;      // En producción: Hasheado

        [BsonElement("Role")]
        public string Role { get; set; } = string.Empty;
    }
}