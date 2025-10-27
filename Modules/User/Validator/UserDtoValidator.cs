using FluentValidation;
using RealEstate.API.Modules.User.Dto;

namespace RealEstate.API.Modules.User.Validator
{
    public class UserDtoValidator : AbstractValidator<UserDto>
    {
        public UserDtoValidator()
        {
            // ===========================================================
            // Id (opcional, formato ObjectId)
            // ===========================================================
            RuleFor(x => x.Id)
                .Matches("^[a-fA-F0-9]{24}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Id))
                .WithMessage("El Id no tiene un formato válido de ObjectId.");

            // ===========================================================
            // Nombre
            // ===========================================================
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre no puede estar vacío.")
                .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.")
                .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("El nombre solo puede contener letras y espacios.");

            // ===========================================================
            // Email
            // ===========================================================
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email no puede estar vacío.")
                .EmailAddress().WithMessage("Debe ser un email válido.")
                .MaximumLength(150).WithMessage("El email no puede superar los 150 caracteres.");

            // ===========================================================
            // Password (mínimo 6 caracteres si se envía)
            // ===========================================================
            RuleFor(x => x.Password)
                .MinimumLength(6)
                .When(x => !string.IsNullOrWhiteSpace(x.Password))
                .WithMessage("La contraseña debe tener al menos 6 caracteres.");

            // ===========================================================
            // Rol
            // ===========================================================
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("El rol no puede estar vacío.")
                .Must(r => new[] { "user", "editor", "admin" }.Contains(r.ToLower()))
                .WithMessage("El rol debe ser 'user', 'editor' o 'admin'.");
        }
    }
}
