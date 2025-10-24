using FluentValidation;
using RealEstate.API.Modules.Owner.Dto;

namespace RealEstate.API.Modules.Owner.Validator
{
    public class OwnerDtoValidator : AbstractValidator<OwnerDto>
    {
        public OwnerDtoValidator()
        {
            RuleFor(o => o.Name).NotEmpty().WithMessage("El nombre del propietario es obligatorio");
            RuleFor(o => o.Address).NotEmpty().WithMessage("La dirección del propietario es obligatoria");
            RuleFor(o => o.Photo).NotEmpty().WithMessage("La foto del propietario es obligatoria");
            RuleFor(o => o.Birthday).NotEmpty().WithMessage("La fecha de nacimiento del propietario es obligatoria");
        }
    }
}
