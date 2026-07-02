using FluentValidation;

namespace InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister;

public class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SupplierId).GreaterThan(0).When(x => x.SupplierId.HasValue);
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}
