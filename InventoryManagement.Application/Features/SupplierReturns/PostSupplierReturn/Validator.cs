using FluentValidation;

namespace InventoryManagement.Application.Features.SupplierReturns.PostSupplierReturn
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
