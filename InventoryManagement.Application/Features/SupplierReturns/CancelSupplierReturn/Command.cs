using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.CancelSupplierReturn
{
    public class Command : IRequest<SupplierReturnResponse>
    {
        public int Id { get; set; }
    }
}
