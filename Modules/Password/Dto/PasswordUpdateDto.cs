namespace RealEstate.API.Modules.Password.Dto
{
    public class PasswordUpdateDto
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}

