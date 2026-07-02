using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.InventoryReports.GetPurchaseRegister;

public class Response
{
    public int PurchaseId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public PurchaseStatus Status { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public IReadOnlyList<ProductLine> Products { get; set; } = Array.Empty<ProductLine>();
}

public class ProductLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
