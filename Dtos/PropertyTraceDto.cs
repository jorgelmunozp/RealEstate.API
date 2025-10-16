namespace RealEstate.API.Dtos
{
    public class PropertyTraceDto
    {
        public string IdPropertyTrace { get; set; } = string.Empty;
        public string DateSale { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Tax { get; set; }
    }
}
