using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace InventoryManagement.Application.Features.Auth.Register
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty()
                .MinimumLength(3);


            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Passward)
                .NotEmpty()
                .MinimumLength(6);
        }
    }
}