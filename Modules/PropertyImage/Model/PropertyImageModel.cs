using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.PropertyImage.Model
{
    public class PropertyImageModel
    {
        // MongoDB _id (interno)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("idPropertyImage")]
        public string IdPropertyImage { get; set; } = string.Empty;

        [BsonElement("file")]
        public string File { get; set; } = string.Empty;

        [BsonElement("enabled")]
        public bool Enabled { get; set; } = true;

        [BsonElement("idProperty")]
        public string IdProperty { get; set; } = string.Empty;        
    }
}
