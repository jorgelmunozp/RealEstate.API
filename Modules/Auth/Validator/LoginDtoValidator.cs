using FluentValidation;
using RealEstate.API.Modules.Auth.Dto;

namespace RealEstate.API.Modules.Auth.Validator
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            // Email obligatorio y formato válido
            RuleFor(x => x.Email).NotEmpty().WithMessage("El email es obligatorio")
                                            .EmailAddress().WithMessage("El email no tiene un formato válido");

            // Password obligatorio y mínimo de 6 caracteres
            RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es obligatoria");
                                    // .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres");
        }
    }
}
