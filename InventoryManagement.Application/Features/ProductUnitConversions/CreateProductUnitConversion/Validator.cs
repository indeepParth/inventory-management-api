using FluentValidation;

namespace InventoryManagement.Application.Features.ProductUnitConversions.CreateProductUnitConversion
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.UnitId).GreaterThan(0);
            RuleFor(x => x.FactorToBaseUnit).GreaterThan(0);
        }
    }
}
