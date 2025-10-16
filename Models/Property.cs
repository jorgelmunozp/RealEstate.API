using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Models
{
    public class Property
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;  // <-- MongoDB identifica el _id del documento

        [BsonElement("idProperty")]
        public string IdProperty { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("codeInternal")]
        public int CodeInternal { get; set; }

        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("owner")]
        public Owner? Owner { get; set; }

        [BsonElement("images")]
        public List<PropertyImage>? Images { get; set; }

        [BsonElement("traces")]
        public List<PropertyTrace>? Traces { get; set; }
    }
}
