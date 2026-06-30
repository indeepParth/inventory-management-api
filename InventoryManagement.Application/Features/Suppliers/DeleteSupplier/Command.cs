using MediatR;

namespace InventoryManagement.Application.Features.Suppliers.DeleteSupplier
{
    public class Command : IRequest<Response>
    {
        public int Id { get; set; }
    }
}
