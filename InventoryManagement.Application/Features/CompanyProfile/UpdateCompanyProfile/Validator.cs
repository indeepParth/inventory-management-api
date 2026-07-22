using FluentValidation;

namespace InventoryManagement.Application.Features.CompanyProfile.UpdateCompanyProfile
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.GstNumber).MaximumLength(15);
            RuleFor(x => x.Email)
                .MaximumLength(254)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.Website)
                .MaximumLength(200)
                .Must(BeValidHttpUrl)
                .WithMessage("Website must be a valid http or https URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.Website));
        }

        private static bool BeValidHttpUrl(string? website)
        {
            return Uri.TryCreate(website, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
