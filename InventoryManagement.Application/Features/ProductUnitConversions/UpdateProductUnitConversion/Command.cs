using MediatR;

namespace InventoryManagement.Application.Features.ProductUnitConversions.UpdateProductUnitConversion
{
    public sealed record Command(
        int ProductId,
        int Id,
        decimal FactorToBaseUnit,
        bool IsActive
    ) : IRequest<Response>;
}
