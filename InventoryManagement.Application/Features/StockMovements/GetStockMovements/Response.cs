using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.StockMovements.GetStockMovements
{
    public class Response
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public StockMovementType MovementType { get; set; }
        public decimal QuantityChange { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal UnitCost { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string? SourceId { get; set; }
        public string? Reference { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
