using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.CustomerReturns
{
    public class CustomerReturnResponse
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int SalesInvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public CustomerReturnStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<CustomerReturnItemResponse> Items { get; set; } = new();
    }

    public class CustomerReturnItemResponse
    {
        public int Id { get; set; }
        public int SalesInvoiceItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal SellingUnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public decimal CostAtSale { get; set; }
    }
}
