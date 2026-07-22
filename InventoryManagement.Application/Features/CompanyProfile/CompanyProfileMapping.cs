using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.CompanyProfile
{
    public static class CompanyProfileMapping
    {
        public static CompanyProfileResponse ToResponse(this Domain.Entities.CompanyProfile? profile)
        {
            if (profile is null)
            {
                return new CompanyProfileResponse();
            }

            return new CompanyProfileResponse
            {
                CompanyName = profile.CompanyName,
                Address = profile.Address,
                GstNumber = profile.GstNumber,
                Email = profile.Email,
                Phone = profile.Phone,
                Website = profile.Website,
                CreatedAtUtc = profile.CreatedAtUtc,
                UpdatedAtUtc = profile.UpdatedAtUtc
            };
        }

        public static Domain.Entities.CompanyProfile ToEntity(
            string companyName,
            string? address,
            string? gstNumber,
            string? email,
            string? phone,
            string? website)
        {
            return new Domain.Entities.CompanyProfile
            {
                CompanyName = companyName.Trim(),
                Address = NormalizeOptional(address),
                GstNumber = NormalizeOptional(gstNumber),
                Email = NormalizeOptional(email),
                Phone = NormalizeOptional(phone),
                Website = NormalizeOptional(website)
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
