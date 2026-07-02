using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;

public class Response
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public UnitOfMeasure Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal StockValue { get; set; }
    public decimal DefaultSellingPrice { get; set; }
}
