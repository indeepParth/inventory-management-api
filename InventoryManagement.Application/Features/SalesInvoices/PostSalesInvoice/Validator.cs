using FluentValidation;

namespace InventoryManagement.Application.Features.SalesInvoices.PostSalesInvoice
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
