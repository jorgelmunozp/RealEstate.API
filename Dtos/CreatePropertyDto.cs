namespace RealEstate.API.Dtos
{
    public class CreatePropertyDto
    {
        public string IdProperty { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public long Price { get; set; }
        public int CodeInternal { get; set; }
        public int Year { get; set; }
        public OwnerDto? Owner { get; set; }
        public List<PropertyImageDto>? Images { get; set; }
        public List<PropertyTraceDto>? Traces { get; set; }
    }
}