using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace InventoryManagement.Application.Features.Auth.RefreshAccessToken
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken)
            .NotEmpty();
        }
    }
}