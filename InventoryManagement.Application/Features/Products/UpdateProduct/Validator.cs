using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace InventoryManagement.Application.Features.Products.UpdateProduct
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty();

            RuleFor(x => x.SKU)
                .NotEmpty();

            RuleFor(x => x.Price)
                .GreaterThan(0);

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0);
        }
    }
}