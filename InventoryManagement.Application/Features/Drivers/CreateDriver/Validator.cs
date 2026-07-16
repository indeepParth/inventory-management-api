using FluentValidation;

namespace InventoryManagement.Application.Features.Drivers.CreateDriver
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.LicenseNumber).MaximumLength(100);
        }
    }
}
