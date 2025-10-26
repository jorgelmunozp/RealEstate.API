namespace RealEstate.API.Modules.PropertyImage.Dto
{
    public class PropertyImageDto
    {
        public string? IdPropertyImage { get; set; }

        public string File { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;

        public string IdProperty { get; set; } = string.Empty;
    }
}
