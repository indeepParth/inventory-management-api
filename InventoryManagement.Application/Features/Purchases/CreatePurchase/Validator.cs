using FluentValidation;

namespace InventoryManagement.Application.Features.Purchases.CreatePurchase
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SupplierId).GreaterThan(0);
            RuleFor(x => x.BillDate).NotEmpty();
            RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.OtherCharges).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(new PurchaseItemValidator());
        }
    }

    public class PurchaseItemValidator : AbstractValidator<PurchaseItemInput>
    {
        public PurchaseItemValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TaxRate).GreaterThanOrEqualTo(0);
        }
    }
}
