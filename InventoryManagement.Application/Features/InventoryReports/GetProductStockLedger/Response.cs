using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.InventoryReports.GetProductStockLedger;

public class Response
{
    public int Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public StockMovementType MovementType { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? SourceId { get; set; }
    public string? SourceReference { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal RunningBalance { get; set; }
}
