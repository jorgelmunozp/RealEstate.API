using FluentValidation;
using RealEstate.API.Modules.PropertyTrace.Dto;
using System.Collections.Generic;

namespace RealEstate.API.Modules.PropertyTrace.Validator
{
    // ✅ Validator para un arreglo de PropertyTraceDto
    public class PropertyTraceArrayValidator : AbstractValidator<IEnumerable<PropertyTraceDto>>
    {
        public PropertyTraceArrayValidator()
        {
            RuleForEach(traces => traces).SetValidator(new PropertyTraceDtoValidator());
        }
    }

    // Validator individual sigue igual
    public class PropertyTraceDtoValidator : AbstractValidator<PropertyTraceDto>
    {
        public PropertyTraceDtoValidator()
        {
            RuleFor(p => p.DateSale).NotEmpty().WithMessage("La fecha de venta es obligatoria");
            RuleFor(p => p.Name).NotEmpty().WithMessage("El nombre es obligatorio");
            RuleFor(p => p.Value).GreaterThan(0).WithMessage("El valor debe ser mayor a 0");
            RuleFor(p => p.Tax).GreaterThanOrEqualTo(0).WithMessage("El impuesto no puede ser negativo");
        }
    }
}
