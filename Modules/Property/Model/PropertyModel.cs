using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.Property.Model
{
    public class PropertyModel
    {
        // MongoDB _id (interno)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        // ID de negocio, único para tu app
        [BsonElement("idProperty")]
        public string IdProperty { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("price")]
        public long Price { get; set; }

        [BsonElement("codeInternal")]
        public int CodeInternal { get; set; }

        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("owner")]
        public string IdOwner { get; set; } = string.Empty;
    }
}
