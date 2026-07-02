using FluentValidation;

namespace InventoryManagement.Application.Features.SupplierReturns.CancelSupplierReturn
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
