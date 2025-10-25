using FluentValidation;
using RealEstate.API.Modules.PropertyImage.Dto;

namespace RealEstate.API.Modules.PropertyImage.Validator
{
    public class PropertyImageDtoValidator : AbstractValidator<PropertyImageDto>
    {
        public PropertyImageDtoValidator()
        {
            // 🔹 Validaciones generales
            RuleFor(p => p.Enabled)
                .NotNull()
                .WithMessage("El estado Enabled no puede ser nulo.");

            // 🔹 Creación (POST)
            When(IsCreateOperation, () =>
            {
                RuleFor(p => p.File)
                    .NotEmpty().WithMessage("La imagen de la propiedad es obligatoria al crear.");

                RuleFor(p => p.IdProperty)
                    .NotEmpty().WithMessage("El Id de la propiedad es obligatorio al crear.");
            });

            // 🔹 Actualización (PUT / PATCH)
            When(IsUpdateOperation, () =>
            {
                RuleFor(p => p.File)
                    .Must(f => string.IsNullOrEmpty(f) || f.Length > 0)
                    .WithMessage("El archivo de imagen enviado no es válido.");

                RuleFor(p => p.IdProperty)
                    .Length(0, 50)
                    .When(p => !string.IsNullOrEmpty(p.IdProperty))
                    .WithMessage("El Id de la propiedad no puede exceder 50 caracteres.");
            });
        }

        private bool IsCreateOperation(PropertyImageDto dto) =>
            string.IsNullOrEmpty(dto.IdPropertyImage);

        private bool IsUpdateOperation(PropertyImageDto dto) =>
            !string.IsNullOrEmpty(dto.IdPropertyImage);
    }
}
