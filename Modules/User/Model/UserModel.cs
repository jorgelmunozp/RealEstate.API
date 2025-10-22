using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.User.Model
{
    public class UserModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        [BsonElement("Id")]
        public required string Id { get; set; }

        [BsonElement("Name")]
        public required string Name { get; set; }

        [BsonElement("Email")]
        public required string Email { get; set; }

        [BsonElement("Password")]
        public required string Password { get; set; }        // En producción: Hasheado

        [BsonElement("Role")]
        public required string Role { get; set; }
    }
}