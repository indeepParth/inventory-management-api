using FluentValidation;

namespace InventoryManagement.Application.Features.Payments.CreatePayment
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReceiptNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue);
            RuleFor(x => x.SalesInvoiceId).GreaterThan(0).When(x => x.SalesInvoiceId.HasValue);
            RuleFor(x => x.SupplierId).GreaterThan(0).When(x => x.SupplierId.HasValue);
            RuleFor(x => x.PurchaseId).GreaterThan(0).When(x => x.PurchaseId.HasValue);
            RuleFor(x => x).Must(x => x.CustomerId.HasValue != x.SupplierId.HasValue)
                .WithMessage("Exactly one of CustomerId or SupplierId is required.");
            RuleFor(x => x).Must(x => !x.SalesInvoiceId.HasValue || x.CustomerId.HasValue)
                .WithMessage("SalesInvoiceId requires CustomerId.");
            RuleFor(x => x).Must(x => !x.PurchaseId.HasValue || x.SupplierId.HasValue)
                .WithMessage("PurchaseId requires SupplierId.");
            RuleFor(x => x.PaymentDate).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Method).IsInEnum();
            RuleFor(x => x.ExternalReference).MaximumLength(150);
            RuleFor(x => x.Note).MaximumLength(1000);
        }
    }
}
