using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.API.Models
{
    public class PropertyTrace
    {
        [BsonElement("idPropertyTrace")]
        public string IdPropertyTrace { get; set; } = string.Empty;

        [BsonElement("dateSale")]
        public string DateSale { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("value")]
        public decimal Value { get; set; }

        [BsonElement("tax")]
        public decimal Tax { get; set; }
    }
}
