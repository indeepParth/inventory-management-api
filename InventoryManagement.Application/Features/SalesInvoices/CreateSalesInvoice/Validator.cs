using FluentValidation;

namespace InventoryManagement.Application.Features.SalesInvoices.CreateSalesInvoice
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CustomerId).GreaterThan(0);
            RuleFor(x => x.InvoiceDate).NotEmpty();
            RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OtherCharges).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(new SalesInvoiceItemValidator());
        }
    }

    public class SalesInvoiceItemValidator : AbstractValidator<SalesInvoiceItemInput>
    {
        public SalesInvoiceItemValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.SellingUnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TaxRate).InclusiveBetween(0, 100);
            RuleFor(x => x.DeliveryChallanItemId)
                .GreaterThan(0)
                .When(x => x.DeliveryChallanItemId.HasValue);
        }
    }
}
