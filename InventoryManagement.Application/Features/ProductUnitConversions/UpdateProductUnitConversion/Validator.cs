using FluentValidation;

namespace InventoryManagement.Application.Features.ProductUnitConversions.UpdateProductUnitConversion
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.FactorToBaseUnit).GreaterThan(0);
        }
    }
}
