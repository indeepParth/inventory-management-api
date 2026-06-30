using FluentValidation;

namespace InventoryManagement.Application.Features.Suppliers.UpdateSupplier
{
    public class Validator : AbstractValidator<Command>
    {
        private const string GstPattern = "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$";

        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.ContactPerson).MaximumLength(150);
            RuleFor(x => x.Email)
                .EmailAddress()
                .MaximumLength(254)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.GstNumber)
                .Must(value => value is null || System.Text.RegularExpressions.Regex.IsMatch(value.Trim().ToUpperInvariant(), GstPattern))
                .WithMessage("GST number must be a valid 15-character Indian GSTIN.")
                .When(x => !string.IsNullOrWhiteSpace(x.GstNumber));
        }
    }
}
