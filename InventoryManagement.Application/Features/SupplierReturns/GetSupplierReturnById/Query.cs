using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.GetSupplierReturnById
{
    public class Query : IRequest<SupplierReturnResponse>
    {
        public int Id { get; set; }
    }
}
