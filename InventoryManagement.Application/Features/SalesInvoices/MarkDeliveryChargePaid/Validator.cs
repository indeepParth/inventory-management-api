using FluentValidation;

namespace InventoryManagement.Application.Features.SalesInvoices.MarkDeliveryChargePaid
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
