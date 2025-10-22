using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Modules.PropertyTrace.Model
{
    public class PropertyTraceModel
    {
        // MongoDB _id (interno)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("DateSale")]
        public string DateSale { get; set; } = string.Empty;

        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Value")]
        public long Value { get; set; }

        [BsonElement("Tax")]
        public long Tax { get; set; }
                
        [BsonElement("IdProperty")]
        public string IdProperty { get; set; } = string.Empty;
    }
}
