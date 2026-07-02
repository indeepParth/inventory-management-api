using FluentValidation;

namespace InventoryManagement.Application.Features.StockMovements.RecordAdjustment
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.QuantityChange).NotEqual(0);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Reference).MaximumLength(150);
            RuleFor(x => x.Note).MaximumLength(1000);
        }
    }
}
