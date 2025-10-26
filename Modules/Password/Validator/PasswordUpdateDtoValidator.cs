using FluentValidation;
using RealEstate.API.Modules.Password.Dto;

namespace RealEstate.API.Modules.Password.Validator
{
    public class PasswordUpdateDtoValidator : AbstractValidator<PasswordUpdateDto>
    {
        public PasswordUpdateDtoValidator()
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage("El token es obligatorio");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La contraseña nueva es obligatoria")
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres");
        }
    }
}

