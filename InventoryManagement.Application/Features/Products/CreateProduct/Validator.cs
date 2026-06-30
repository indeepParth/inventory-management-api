using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace InventoryManagement.Application.Features.Products.CreateProduct
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty();

            RuleFor(x => x.SKU)
                .NotEmpty();

            RuleFor(x => x.DefaultSellingPrice)
                .GreaterThan(0);

            RuleFor(x => x.BaseUnit)
                .IsInEnum();

            RuleFor(x => x.CategoryId)
                .GreaterThan(0);
        }
    }
}
