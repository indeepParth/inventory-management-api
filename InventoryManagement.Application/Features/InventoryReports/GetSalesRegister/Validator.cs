using FluentValidation;

namespace InventoryManagement.Application.Features.InventoryReports.GetSalesRegister;

public class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue);
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
        RuleFor(x => x.SourceType).IsInEnum().When(x => x.SourceType.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}
