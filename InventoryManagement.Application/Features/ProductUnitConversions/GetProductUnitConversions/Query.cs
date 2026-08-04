using MediatR;

namespace InventoryManagement.Application.Features.ProductUnitConversions.GetProductUnitConversions
{
    public class Query : IRequest<List<Response>>
    {
        public int ProductId { get; set; }
    }
}
