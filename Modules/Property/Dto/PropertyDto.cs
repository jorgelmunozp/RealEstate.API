using RealEstate.API.Modules.PropertyImage.Dto;

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
        public string IdOwner { get; set; } = string.Empty;

        // 🔹 Nueva propiedad para incluir imagen
        public PropertyImageDto? Image { get; set; }
    }
}
