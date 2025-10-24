using Microsoft.AspNetCore.Http;

namespace RealEstate.API.Modules.PropertyImage.Dto
{
    public class PropertyImageDto
    {

        // Identificador único (MongoDB _id) - Opcional al crear, obligatorio al actualizar
        public string? Id { get; set; }        public string File { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string IdProperty { get; set; } = string.Empty;
    }
}
