using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.PostCustomerReturn
{
    public class Command : IRequest<CustomerReturnResponse>
    {
        public int Id { get; set; }
    }
}
