using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.Suppliers
{
    internal static class SupplierMapping
    {
        public static SupplierResponse ToResponse(this Supplier supplier)
        {
            return new SupplierResponse
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address,
                GstNumber = supplier.GstNumber,
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt
            };
        }

        public static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
