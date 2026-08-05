namespace InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;

public class Response
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public int? BaseProductId { get; set; }
    public string? BaseProductName { get; set; }
    public decimal? FactorToBaseProduct { get; set; }
    public bool IsSubProduct { get; set; }
    public decimal AverageCost { get; set; }
    public decimal StockValue { get; set; }
    public decimal DefaultSellingPrice { get; set; }
}
