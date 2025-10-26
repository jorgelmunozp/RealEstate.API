using FluentValidation;
using RealEstate.API.Modules.Password.Dto;

namespace RealEstate.API.Modules.Password.Validator
{
    public class PasswordRecoverDtoValidator : AbstractValidator<PasswordRecoverDto>
    {
        public PasswordRecoverDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("El email es obligatorio")
                                 .EmailAddress().WithMessage("Email inválido");
        }
    }
}

