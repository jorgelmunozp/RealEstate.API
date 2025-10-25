using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.PropertyImage.Model
{
    public class PropertyImageModel
    {
        // 🔹 MongoDB _id (interno)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdPropertyImage { get; set; } = string.Empty;

        [BsonElement("File")]
        public string File { get; set; } = string.Empty;

        [BsonElement("Enabled")]
        public bool Enabled { get; set; } = true;

        [BsonElement("IdProperty")]
        public string IdProperty { get; set; } = string.Empty;
    }
}
