using FluentValidation;
using RealEstate.API.Modules.PropertyImage.Dto;

namespace RealEstate.API.Modules.PropertyImage.Validator
{
    public class PropertyImageDtoValidator : AbstractValidator<PropertyImageDto>
    {
        public PropertyImageDtoValidator()
        {
            // ===========================================================
            // 🔹 Validación común para todas las operaciones
            // ===========================================================
            RuleFor(p => p.Enabled)
                .NotNull()
                .WithMessage("El campo 'Enabled' no puede ser nulo.");

            // ===========================================================
            // 🔹 Validación específica para creación (POST)
            // ===========================================================
            When(IsCreateOperation, () =>
            {
                RuleFor(p => p.File)
                    .NotEmpty()
                    .WithMessage("El archivo de imagen es obligatorio al crear una nueva propiedad.");

                RuleFor(p => p.IdProperty)
                    .NotEmpty()
                    .WithMessage("El IdProperty es obligatorio al crear una imagen.");
            });

            // ===========================================================
            // 🔹 Validación específica para actualización (PUT / PATCH)
            // ===========================================================
            When(IsUpdateOperation, () =>
            {
                RuleFor(p => p.IdPropertyImage)
                    .NotEmpty()
                    .WithMessage("El IdPropertyImage es obligatorio para actualizar una imagen.");

                // Solo validar contenido del archivo si se envía
                RuleFor(p => p.File)
                    .Must(f => string.IsNullOrEmpty(f) || f.Length > 10)
                    .WithMessage("El archivo de imagen enviado no es válido.");

                // Validar IdProperty si se intenta modificar
                RuleFor(p => p.IdProperty)
                    .MaximumLength(50)
                    .When(p => !string.IsNullOrEmpty(p.IdProperty))
                    .WithMessage("El IdProperty no puede exceder los 50 caracteres.");
            });
        }

        // ===========================================================
        // 🔹 Helpers para distinguir tipo de operación
        // ===========================================================
        private static bool IsCreateOperation(PropertyImageDto dto) =>
            string.IsNullOrEmpty(dto.IdPropertyImage);

        private static bool IsUpdateOperation(PropertyImageDto dto) =>
            !string.IsNullOrEmpty(dto.IdPropertyImage);
    }
}
