using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Models
{
    public class Owner
    {
        [BsonElement("idOwner")]
        public string IdOwner { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("photo")]
        public string Photo { get; set; } = string.Empty;

        [BsonElement("birthday")]
        public string Birthday { get; set; } = string.Empty;
    }
}
