using FluentValidation;

namespace InventoryManagement.Application.Features.DeliveryChallans.PostDeliveryChallan
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.Id).GreaterThan(0);
    }
}
