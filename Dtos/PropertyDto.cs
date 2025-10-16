namespace RealEstate.API.Dtos
{
    public class PropertyDto
    {
        public string Id { get; set; } = string.Empty;
        public string IdOwner { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AddressProperty { get; set; } = string.Empty;
        public decimal PriceProperty { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
