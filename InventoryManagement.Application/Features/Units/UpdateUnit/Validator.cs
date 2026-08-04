using FluentValidation;

namespace InventoryManagement.Application.Features.Units.UpdateUnit
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.ShortName)
                .MaximumLength(20);

            RuleFor(x => x.FactorToBaseUnit)
                .GreaterThan(0);

            RuleFor(x => x.FactorToBaseUnit)
                .Equal(1)
                .When(x => !x.BaseUnitId.HasValue || x.BaseUnitId == x.Id)
                .WithMessage("Base unit factor must be 1.");
        }
    }
}
