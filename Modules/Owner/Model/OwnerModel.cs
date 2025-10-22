using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.Owner.Model
{
    public class OwnerModel
    {
        // MongoDB _id (interno)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("Photo")]
        public string Photo { get; set; } = string.Empty;

        [BsonElement("Birthday")]
        public string Birthday { get; set; } = string.Empty;
    }
}
