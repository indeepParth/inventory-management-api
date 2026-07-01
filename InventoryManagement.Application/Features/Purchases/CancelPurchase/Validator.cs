using FluentValidation;

namespace InventoryManagement.Application.Features.Purchases.CancelPurchase
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
