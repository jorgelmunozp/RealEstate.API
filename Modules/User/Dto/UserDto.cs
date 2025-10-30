namespace RealEstate.API.Modules.User.Dto
{
    public class UserDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }       // Solo requerido en creación o cambio de contraseña
        public string Role { get; set; } = "user";  // Rol del usuario (por defecto 'user')
    }
}
