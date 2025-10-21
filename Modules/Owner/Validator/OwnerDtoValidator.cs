using FluentValidation;
using RealEstate.API.Modules.Owner.Dto;

namespace RealEstate.API.Modules.Owner.Validator
{

    // ✅ Validator para OwnerDto
    public class OwnerDtoValidator : AbstractValidator<OwnerDto>
    {
        public OwnerDtoValidator()
        {
            RuleFor(o => o.IdOwner).NotEmpty().WithMessage("El Id del propietario es obligatorio");
            RuleFor(o => o.Name).NotEmpty().WithMessage("El nombre es obligatorio");
            RuleFor(o => o.Address).NotEmpty().WithMessage("La dirección es obligatoria");
            RuleFor(o => o.Photo).NotEmpty().WithMessage("La foto es obligatoria");
            RuleFor(o => o.Birthday).NotEmpty().WithMessage("El cumpleaños es obligatorio");
        }
    }
}
