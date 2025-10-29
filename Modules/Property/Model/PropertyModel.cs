using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyTrace.Dto;

namespace RealEstate.API.Modules.Property.Model
{
    public class PropertyModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("Address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("Price")]
        public long Price { get; set; }

        [BsonElement("CodeInternal")]
        public int CodeInternal { get; set; }

        [BsonElement("Year")]
        public int Year { get; set; }

        [BsonElement("IdOwner")]
        public string IdOwner { get; set; } = string.Empty;

              // Campo virtual (no se guarda en la base de datos)
              [BsonIgnore]
        public OwnerDto? Owner { get; set; }
        [BsonIgnore]
        public PropertyImageDto? Image { get; set; }
        [BsonIgnore]
        public PropertyTraceDto? Traces { get; set; }                
    }
}
