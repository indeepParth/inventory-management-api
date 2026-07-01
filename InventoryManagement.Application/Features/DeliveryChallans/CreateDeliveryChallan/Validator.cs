using FluentValidation;

namespace InventoryManagement.Application.Features.DeliveryChallans.CreateDeliveryChallan
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChallanNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CustomerId).GreaterThan(0);
            RuleFor(x => x.ChallanDate).NotEmpty();
            RuleFor(x => x.VehicleNumber).MaximumLength(50);
            RuleFor(x => x.DriverName).MaximumLength(150);
            RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Notes).MaximumLength(1000);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(new ItemValidator());
        }
    }

    public class ItemValidator : AbstractValidator<DeliveryChallanItemInput>
    {
        public ItemValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
