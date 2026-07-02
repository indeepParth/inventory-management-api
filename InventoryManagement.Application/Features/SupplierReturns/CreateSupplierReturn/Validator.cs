using FluentValidation;

namespace InventoryManagement.Application.Features.SupplierReturns.CreateSupplierReturn
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReturnNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.PurchaseId).GreaterThan(0);
            RuleFor(x => x.ReturnDate).NotEmpty();
            RuleFor(x => x.Notes).MaximumLength(1000);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(
                new SupplierReturnItemValidator());
        }
    }

    public class SupplierReturnItemValidator :
        AbstractValidator<SupplierReturnItemInput>
    {
        public SupplierReturnItemValidator()
        {
            RuleFor(x => x.PurchaseItemId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
