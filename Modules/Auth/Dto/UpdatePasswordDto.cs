namespace RealEstate.API.Modules.Auth.Dto
{
    public class UpdatePasswordDto
    {
        public string UserId { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}