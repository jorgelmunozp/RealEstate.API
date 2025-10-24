using FluentValidation;
using RealEstate.API.Modules.Property.Model;
using System;

namespace RealEstate.API.Modules.Property.Validator
{
    // Validator para PropertyModel
    public class PropertyModelValidator : AbstractValidator<PropertyModel>
    {
        public PropertyModelValidator()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("El Id de la propiedad es obligatorio");
            RuleFor(p => p.Name).NotEmpty().WithMessage("El nombre de la propiedad es obligatorio");
            RuleFor(p => p.Address).NotEmpty().WithMessage("La dirección de la propiedad es obligatoria");
            RuleFor(p => p.Price).GreaterThan(0) .WithMessage("El precio debe ser mayor a 0");
            RuleFor(p => p.CodeInternal).GreaterThan(0).WithMessage("El código interno debe ser mayor a 0");
            RuleFor(p => p.Year).InclusiveBetween(1800, DateTime.Now.Year).WithMessage($"El año debe estar entre 1800 y {DateTime.Now.Year}");
            RuleFor(p => p.IdOwner).NotEmpty().WithMessage("El Id del propietario es obligatorio");
        }

        // Método auxiliar para validar una instancia manualmente
        public bool ValidateModel(PropertyModel model, out List<string> errors)
        {
            var result = Validate(model);
            errors = new List<string>();

            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                {
                    errors.Add(failure.ErrorMessage);
                }
            }

            return result.IsValid;
        }
    }
}
