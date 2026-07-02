using FluentValidation;

namespace InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;

public class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.Stock).IsInEnum().When(x => x.Stock.HasValue);
    }
}
