namespace RealEstate.API.Modules.User.Dto
{
    public class UserDto
    {
        // ===========================================================
        // Identificador (opcional, se usa en respuestas o edición)
        // ===========================================================
        public string? Id { get; set; }

        // ===========================================================
        // Datos principales
        // ===========================================================
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // ⚙️ Solo requerido en creación o cambio de contraseña
        public string? Password { get; set; }

        // ===========================================================
        // Rol del usuario (por defecto 'user')
        // ===========================================================
        public string Role { get; set; } = "user";
    }
}
