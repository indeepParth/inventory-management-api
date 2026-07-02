using FluentValidation;

namespace InventoryManagement.Application.Features.Payments.CreatePayment
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReceiptNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CustomerId).GreaterThan(0);
            RuleFor(x => x.SalesInvoiceId).GreaterThan(0).When(x => x.SalesInvoiceId.HasValue);
            RuleFor(x => x.PaymentDate).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Method).IsInEnum();
            RuleFor(x => x.ExternalReference).MaximumLength(150);
            RuleFor(x => x.Note).MaximumLength(1000);
        }
    }
}
