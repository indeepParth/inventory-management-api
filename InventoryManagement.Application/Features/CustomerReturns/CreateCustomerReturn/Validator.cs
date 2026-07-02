using FluentValidation;

namespace InventoryManagement.Application.Features.CustomerReturns.CreateCustomerReturn
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReturnNumber).NotEmpty().MaximumLength(50);
            RuleFor(x => x.SalesInvoiceId).GreaterThan(0);
            RuleFor(x => x.ReturnDate).NotEmpty();
            RuleFor(x => x.Notes).MaximumLength(1000);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).SetValidator(
                new CustomerReturnItemValidator());
        }
    }

    public class CustomerReturnItemValidator :
        AbstractValidator<CustomerReturnItemInput>
    {
        public CustomerReturnItemValidator()
        {
            RuleFor(x => x.SalesInvoiceItemId).GreaterThan(0);
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
