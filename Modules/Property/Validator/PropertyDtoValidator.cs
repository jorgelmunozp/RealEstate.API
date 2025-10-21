using FluentValidation;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Owner.Validator;
using RealEstate.API.Modules.PropertyImage.Validator;
using RealEstate.API.Modules.PropertyTrace.Validator;

namespace RealEstate.API.Modules.Property.Validator
{
    // ✅ Validator para PropertyDto
    public class PropertyDtoValidator : AbstractValidator<PropertyDto>
    {
        public PropertyDtoValidator()
        {
            RuleFor(p => p.IdProperty).NotEmpty().WithMessage("El Id de la propiedad es obligatorio");
            RuleFor(p => p.Name).NotEmpty().WithMessage("El nombre de la propiedad es obligatorio");
            RuleFor(p => p.Address).NotEmpty().WithMessage("La dirección de la propiedad es obligatoria");
            RuleFor(p => p.Price).GreaterThan(0).WithMessage("El precio debe ser mayor a 0");
            RuleFor(p => p.CodeInternal).GreaterThan(0).WithMessage("El código interno debe ser mayor a 0");
            RuleFor(p => p.Year).InclusiveBetween(1800, DateTime.Now.Year).WithMessage("El año debe estar entre 1800 y el año actual");
            RuleFor(p => p.IdOwner).NotEmpty().WithMessage("El Id del propietario es obligatorio");
        }
    }
}
