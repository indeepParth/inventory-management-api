using FluentValidation;

namespace InventoryManagement.Application.Features.SalesInvoices.CreateFromChallans
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.InvoiceDate).NotEmpty();
            RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OtherCharges).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(new ChallanItemValidator());
        }
    }

    public class ChallanItemValidator : AbstractValidator<ChallanItemInput>
    {
        public ChallanItemValidator()
        {
            RuleFor(x => x.DeliveryChallanItemId).GreaterThan(0);
            RuleFor(x => x.SellingUnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TaxRate).InclusiveBetween(0, 100);
        }
    }
}
