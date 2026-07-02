using FluentValidation;

namespace InventoryManagement.Application.Features.Payments.GetPayments
{
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue);
            RuleFor(x => x.SalesInvoiceId).GreaterThan(0).When(x => x.SalesInvoiceId.HasValue);
            RuleFor(x => x.SupplierId).GreaterThan(0).When(x => x.SupplierId.HasValue);
            RuleFor(x => x.PurchaseId).GreaterThan(0).When(x => x.PurchaseId.HasValue);
            RuleFor(x => x.Method).IsInEnum().When(x => x.Method.HasValue);
            RuleFor(x => x.DateTo)
                .GreaterThanOrEqualTo(x => x.DateFrom!.Value)
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
        }
    }
}
