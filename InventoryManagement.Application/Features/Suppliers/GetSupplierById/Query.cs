using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.GetSupplierById
{
    public class Query : IRequest<SupplierResponse>
    {
        public int Id { get; set; }
    }
}
