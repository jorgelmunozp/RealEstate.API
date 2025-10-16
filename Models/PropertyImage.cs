using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Models
{
    public class PropertyImage
    {
        [BsonElement("idPropertyImage")]
        public string IdPropertyImage { get; set; } = string.Empty;

        [BsonElement("file")]
        public string File { get; set; } = string.Empty;

        [BsonElement("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
