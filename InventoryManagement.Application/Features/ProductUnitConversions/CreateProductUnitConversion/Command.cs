using MediatR;

namespace InventoryManagement.Application.Features.ProductUnitConversions.CreateProductUnitConversion
{
    public class Command : IRequest<Response>
    {
        public int ProductId { get; set; }
        public int UnitId { get; set; }
        public decimal FactorToBaseUnit { get; set; }
    }
}
