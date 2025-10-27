using FluentValidation;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Owner.Validator;

namespace RealEstate.API.Modules.Property.Validator
{
    public class PropertyDtoValidator : AbstractValidator<PropertyDto>
    {
        public PropertyDtoValidator()
        {
            // Reglas básicas
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage("El nombre de la propiedad es obligatorio");

            RuleFor(p => p.Address)
                .NotEmpty()
                .WithMessage("La dirección de la propiedad es obligatoria");

            RuleFor(p => p.Price)
                .GreaterThan(0)
                .WithMessage("El precio debe ser mayor a 0");

            RuleFor(p => p.CodeInternal)
                .GreaterThan(0)
                .WithMessage("El código interno debe ser mayor a 0");

            RuleFor(p => p.Year)
                .InclusiveBetween(1800, DateTime.Now.Year)
                .WithMessage("El año debe estar entre 1800 y el año actual");

            // Validación dual: IdOwner o Owner embebido
            When(p => p.Owner == null, () =>
            {
                RuleFor(p => p.IdOwner)
                    .NotEmpty()
                    .WithMessage("Debe incluir el IdOwner o un objeto Owner válido");
            });

            When(p => p.Owner != null, () =>
            {
                RuleFor(p => p.Owner)
                    .SetValidator(new OwnerDtoValidator())
                    .WithMessage("Los datos del propietario no son válidos");
            });

            // Validación de imagen
            When(p => p.Image != null, () =>
            {
                RuleFor(p => p.Image.File)
                    .NotEmpty()
                    .WithMessage("La imagen de la propiedad no puede estar vacía");
            });

            // Validación de trazas (opcional)
            When(p => p.Traces != null && p.Traces.Any(), () =>
            {
                RuleForEach(p => p.Traces).ChildRules(trace =>
                {
                    trace.RuleFor(t => t.Name)
                        .NotEmpty()
                        .WithMessage("El nombre de la traza es obligatorio");

                    trace.RuleFor(t => t.DateSale)
                        .NotEmpty()
                        .WithMessage("La fecha de venta es obligatoria");
                });
            });
        }
    }
}
