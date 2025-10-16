using FluentValidation;
using RealEstate.API.Dtos;

namespace RealEstate.API.Validators
{
    public class PropertyDtoValidator : AbstractValidator<PropertyDto>
    {
        public PropertyDtoValidator()
        {
            RuleFor(x => x.IdProperty)
                .NotEmpty().WithMessage("El Id del inmueble es obligatorio");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("La dirección es obligatoria")
                .MaximumLength(200).WithMessage("La dirección no puede exceder 200 caracteres");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("El precio debe ser mayor que cero");

            RuleFor(x => x.CodeInternal)
                .GreaterThan(0).WithMessage("El código interno debe ser mayor que cero");

            RuleFor(x => x.Year)
                .InclusiveBetween(1900, 2100).WithMessage("El año debe estar entre 1900 y 2100");

            // Validar Owner completo
            RuleFor(x => x.Owner)
                .NotNull().WithMessage("El propietario es obligatorio")
                .SetValidator(new OwnerDtoValidator());

            // Validar que haya al menos una imagen y cada imagen tenga File
            RuleFor(x => x.Images)
                .NotEmpty().WithMessage("Debe haber al menos una imagen");

            RuleForEach(x => x.Images).SetValidator(new PropertyImageDtoValidator());

            // Traces opcional, se pueden validar con otro validator si quieres
        }
    }

    // Validator para OwnerDto
    public class OwnerDtoValidator : AbstractValidator<OwnerDto>
    {
        public OwnerDtoValidator()
        {
            RuleFor(o => o.IdOwner).NotEmpty().WithMessage("El Id del propietario es obligatorio");
            RuleFor(o => o.Name).NotEmpty().WithMessage("El nombre del propietario es obligatorio");
            // Las demás propiedades (Address, Photo, Birthday) pueden ser opcionales o validadas según necesidad
        }
    }

    // Validator para PropertyImageDto
    public class PropertyImageDtoValidator : AbstractValidator<PropertyImageDto>
    {
        public PropertyImageDtoValidator()
        {
            RuleFor(img => img.File).NotEmpty().WithMessage("La URL de la imagen es obligatoria");
            // Enabled opcional
        }
    }
}
