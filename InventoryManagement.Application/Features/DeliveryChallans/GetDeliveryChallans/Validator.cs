using FluentValidation;

namespace InventoryManagement.Application.Features.DeliveryChallans.GetDeliveryChallans
{
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue);
            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
            RuleFor(x => x.DateTo)
                .GreaterThanOrEqualTo(x => x.DateFrom)
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
        }
    }
}
