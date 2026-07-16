using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.Drivers
{
    internal static class DriverMapping
    {
        public static DriverResponse ToResponse(this Driver driver)
        {
            return new DriverResponse
            {
                Id = driver.Id,
                Name = driver.Name,
                Phone = driver.Phone,
                LicenseNumber = driver.LicenseNumber,
                IsActive = driver.IsActive,
                CreatedAtUtc = driver.CreatedAtUtc,
                UpdatedAtUtc = driver.UpdatedAtUtc
            };
        }

        public static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
