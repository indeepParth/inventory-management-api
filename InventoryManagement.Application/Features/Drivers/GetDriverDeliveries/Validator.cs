using FluentValidation;

namespace InventoryManagement.Application.Features.Drivers.GetDriverDeliveries
{
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DriverId).GreaterThan(0);
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.PaymentStatus).IsInEnum();
            RuleFor(x => x.DateTo)
                .GreaterThanOrEqualTo(x => x.DateFrom)
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
        }
    }
}
