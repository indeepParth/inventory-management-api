using FluentValidation;

namespace InventoryManagement.Application.Features.Products.GetProducts
{
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
