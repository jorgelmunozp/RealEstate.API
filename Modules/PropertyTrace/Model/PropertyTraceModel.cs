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

        [BsonElement("idPropertyTrace")]
        public string IdPropertyTrace { get; set; } = string.Empty;

        [BsonElement("dateSale")]
        public string DateSale { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("value")]
        public long Value { get; set; }

        [BsonElement("tax")]
        public long Tax { get; set; }
                
        [BsonElement("idProperty")]
        public string IdProperty { get; set; } = string.Empty;
    }
}
