using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.Customers
{
    internal static class CustomerMapping
    {
        public static CustomerResponse ToResponse(this Customer customer)
        {
            return new CustomerResponse
            {
                Id = customer.Id,
                Name = customer.Name,
                ContactPerson = customer.ContactPerson,
                Phone = customer.Phone,
                Email = customer.Email,
                BillingAddress = customer.BillingAddress,
                DeliveryAddress = customer.DeliveryAddress,
                GstNumber = customer.GstNumber,
                CreditLimit = customer.CreditLimit,
                BalanceDue = customer.BalanceDue,
                IsActive = customer.IsActive,
                CreatedAtUtc = customer.CreatedAtUtc,
                UpdatedAtUtc = customer.UpdatedAtUtc
            };
        }

        public static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
