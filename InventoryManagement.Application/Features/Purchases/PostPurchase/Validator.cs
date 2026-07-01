using FluentValidation;

namespace InventoryManagement.Application.Features.Purchases.PostPurchase
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
