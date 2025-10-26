using FluentValidation;
using RealEstate.API.Modules.User.Dto;

namespace RealEstate.API.Modules.User.Validator
{
    public class UserDtoValidator : AbstractValidator<UserDto>
    {
        public UserDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre no puede estar vacío.");

            RuleFor(x => x.Email).NotEmpty().WithMessage("El email no puede estar vacío.")
                                 .EmailAddress().WithMessage("Debe ser un email válido.");

            RuleFor(x => x.Password)
                .MinimumLength(6).When(x => !string.IsNullOrWhiteSpace(x.Password))
                .WithMessage("La contraseña debe tener al menos 6 caracteres.");

            RuleFor(x => x.Role).NotEmpty().WithMessage("El rol no puede estar vacío.");
        }
    }
}
