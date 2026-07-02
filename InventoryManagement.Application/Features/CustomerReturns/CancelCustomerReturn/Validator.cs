using FluentValidation;

namespace InventoryManagement.Application.Features.CustomerReturns.CancelCustomerReturn
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
