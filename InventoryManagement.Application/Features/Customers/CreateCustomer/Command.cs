using MediatR;

namespace InventoryManagement.Application.Features.Customers.CreateCustomer
{
    public class Command : IRequest<CustomerResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BillingAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? GstNumber { get; set; }
        public decimal CreditLimit { get; set; }
    }
}
