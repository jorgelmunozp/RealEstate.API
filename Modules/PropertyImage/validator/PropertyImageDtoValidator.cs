using FluentValidation;
using RealEstate.API.Modules.PropertyImage.Dto;

namespace RealEstate.API.Modules.PropertyImage.Validator
{
    // ✅ Validator para PropertyImageDto
    public class PropertyImageDtoValidator : AbstractValidator<PropertyImageDto>
    {
        public PropertyImageDtoValidator()
        {
            RuleFor(p => p.File).NotEmpty().WithMessage("La imagen de la propiedad es obligatoria");
            RuleFor(p => p.IdProperty).NotEmpty().WithMessage("El Id de la propiedad es obligatorio");

            // Enabled no requiere validación obligatoria, ya que tiene valor por defecto (true)
        }
    }
}
