using MediatR;

namespace InventoryManagement.Application.Features.Customers.GetCustomerById
{
    public class Query : IRequest<CustomerResponse>
    {
        public int Id { get; set; }
    }
}
