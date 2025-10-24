namespace RealEstate.API.Modules.PropertyTrace.Dto
{
    public class PropertyTraceDto
    {
        public string IdPropertyTrace { get; set; } = string.Empty;
        public string DateSale { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Value { get; set; }
        public long Tax { get; set; }
        public string IdProperty { get; set; } = string.Empty;

    }
}
