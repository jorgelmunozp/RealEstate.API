using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }
    public required string Name { get; set; } 
    public required string Email { get; set; }
    public required string Password { get; set; }        // En producción: Hasheado
    public required string Role { get; set; }
}