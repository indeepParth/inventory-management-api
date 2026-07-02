using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.InventoryReports.GetSalesRegister;

public class Response
{
    public int SalesInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public SalesSourceType SourceType { get; set; }
    public SalesInvoiceStatus Status { get; set; }
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
