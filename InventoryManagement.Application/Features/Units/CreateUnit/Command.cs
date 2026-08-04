using MediatR;

namespace InventoryManagement.Application.Features.Units.CreateUnit
{
    public class Command : IRequest<InventoryManagement.Application.Features.Units.Response>
    {
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public decimal FactorToBaseUnit { get; set; } = 1m;
        public int? BaseUnitId { get; set; }
    }
}
