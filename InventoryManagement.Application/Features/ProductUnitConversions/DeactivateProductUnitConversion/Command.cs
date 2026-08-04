using MediatR;

namespace InventoryManagement.Application.Features.ProductUnitConversions.DeactivateProductUnitConversion
{
    public class Command : IRequest<Response>
    {
        public int ProductId { get; set; }
        public int Id { get; set; }
    }
}
