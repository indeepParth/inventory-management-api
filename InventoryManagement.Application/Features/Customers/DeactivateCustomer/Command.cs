using MediatR;

namespace InventoryManagement.Application.Features.Customers.DeactivateCustomer
{
    public class Command : IRequest<CustomerResponse>
    {
        public int Id { get; set; }
    }
}
