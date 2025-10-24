using FluentValidation;
using RealEstate.API.Modules.PropertyImage.Dto;

namespace RealEstate.API.Modules.PropertyImage.Validator
{
    // Validator flexible para PropertyImageDto
    // Permite distinguir entre creación y actualización parcial
    public class PropertyImageDtoValidator : AbstractValidator<PropertyImageDto>
    {
        public PropertyImageDtoValidator()
        {
            // 🔹 Validaciones generales (siempre aplican)
            RuleFor(p => p.Enabled)
                .NotNull()
                .WithMessage("El estado Enabled no puede ser nulo.");

            // 🔹 Reglas condicionales: se validan solo en creación (POST)
            When(IsCreateOperation, () =>
            {
                RuleFor(p => p.File)
                    .NotEmpty()
                    .WithMessage("La imagen de la propiedad es obligatoria al crear.");

                RuleFor(p => p.IdProperty)
                    .NotEmpty()
                    .WithMessage("El Id de la propiedad es obligatorio al crear.");
            });

            // 🔹 En PATCH, las propiedades son opcionales (solo se valida si se envían)
            When(IsUpdateOperation, () =>
            {
                RuleFor(p => p.File)
                    .Must(f => f == null || f.Length > 0)
                    .WithMessage("El archivo de imagen enviado no es válido.");

                RuleFor(p => p.IdProperty)
                    .Length(0, 50)
                    .When(p => !string.IsNullOrEmpty(p.IdProperty))
                    .WithMessage("El Id de la propiedad no puede exceder 50 caracteres.");
            });
        }

        // 🔸 Método auxiliar: detecta si es creación (POST)
        private bool IsCreateOperation(PropertyImageDto dto)
        {
            // Si no tiene ID, se asume creación
            return string.IsNullOrEmpty(dto.Id);
        }

        // 🔸 Método auxiliar: detecta si es actualización (PATCH/PUT)
        private bool IsUpdateOperation(PropertyImageDto dto)
        {
            // Si tiene ID, se asume actualización
            return !string.IsNullOrEmpty(dto.Id);
        }
    }
}
