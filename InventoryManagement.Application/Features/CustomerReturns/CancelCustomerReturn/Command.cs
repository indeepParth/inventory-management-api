using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.CancelCustomerReturn
{
    public class Command : IRequest<CustomerReturnResponse>
    {
        public int Id { get; set; }
    }
}
