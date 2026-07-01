using MediatR;

namespace InventoryManagement.Application.Features.Customers.UpdateCustomer
{
    public sealed record Command(
        int Id,
        string Name,
        string? ContactPerson,
        string? Phone,
        string? Email,
        string? BillingAddress,
        string? DeliveryAddress,
        string? GstNumber,
        decimal CreditLimit,
        bool IsActive) : IRequest<CustomerResponse>;
}
