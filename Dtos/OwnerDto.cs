namespace RealEstate.API.Dtos
{
    public class OwnerDto
    {
        public string IdOwner { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Photo { get; set; } = string.Empty;
        public string Birthday { get; set; } = string.Empty; // O DateTime si quieres
    }
}
