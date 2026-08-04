using MediatR;

namespace InventoryManagement.Application.Features.Units.UpdateUnit
{
    public sealed record Command(
        int Id,
        string Name,
        string? ShortName,
        decimal FactorToBaseUnit,
        int? BaseUnitId,
        bool IsActive
    ) : IRequest<InventoryManagement.Application.Features.Units.Response>;
}
