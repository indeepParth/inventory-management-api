using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.PostSupplierReturn
{
    public class Command : IRequest<SupplierReturnResponse>
    {
        public int Id { get; set; }
    }
}
