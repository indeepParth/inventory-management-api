using FluentValidation;

namespace InventoryManagement.Application.Features.Payments.ReversePayment
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.ReceiptNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.PaymentDate).NotEmpty();
            RuleFor(x => x.ExternalReference).MaximumLength(150);
            RuleFor(x => x.Note).MaximumLength(1000);
        }
    }
}
