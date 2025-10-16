namespace RealEstate.API.Dtos
{
    public class PropertyDto
    {
        public string IdProperty { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CodeInternal { get; set; }
        public int Year { get; set; }
        public OwnerDto Owner { get; set; } = new OwnerDto();
        public List<PropertyImageDto> Images { get; set; } = new List<PropertyImageDto>();
        public List<PropertyTraceDto> Traces { get; set; } = new List<PropertyTraceDto>();
    }
}
