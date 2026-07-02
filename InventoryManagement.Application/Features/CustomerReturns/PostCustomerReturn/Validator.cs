using FluentValidation;

namespace InventoryManagement.Application.Features.CustomerReturns.PostCustomerReturn
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
