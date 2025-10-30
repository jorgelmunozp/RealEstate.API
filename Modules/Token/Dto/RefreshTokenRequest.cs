namespace RealEstate.API.Modules.Token.Dto
{
    public class RefreshTokenRequest
    {
        public string? RefreshToken { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
    }
}
