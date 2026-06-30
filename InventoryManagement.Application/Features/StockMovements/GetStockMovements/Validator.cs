using FluentValidation;

namespace InventoryManagement.Application.Features.StockMovements.GetStockMovements
{
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
            RuleFor(x => x.MovementType)
                .IsInEnum()
                .When(x => x.MovementType.HasValue);
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
        }
    }
}
