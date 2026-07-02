using FluentValidation;

namespace InventoryManagement.Application.Features.Statements.CustomerStatement
{
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0);
            RuleFor(x => x.DateFrom).NotEmpty();
            RuleFor(x => x.DateTo).NotEmpty()
                .GreaterThanOrEqualTo(x => x.DateFrom);
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
