using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.GetCustomerReturnById
{
    public class Query : IRequest<CustomerReturnResponse>
    {
        public int Id { get; set; }
    }
}
