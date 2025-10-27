using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyTrace.Dto;

namespace RealEstate.API.Modules.Property.Dto
{
    public class PropertyDto
    {
        public string IdProperty { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public long Price { get; set; }
        public int CodeInternal { get; set; }
        public int Year { get; set; }
        public string? IdOwner { get; set; }

        // Relaciones embebidas opcionales
        public OwnerDto? Owner { get; set; }
        public PropertyImageDto? Image { get; set; }
        public List<PropertyTraceDto>? Traces { get; set; }
    }
}
