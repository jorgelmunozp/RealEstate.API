namespace RealEstate.API.Modules.Token.Dto
{
    public class RefreshTokenRequest
    {
        public string? RefreshToken { get; set; }

        // Información opcional del usuario (útil si no la extraes del DB)
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
    }
}
