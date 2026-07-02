using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.SupplierReturns
{
    public class SupplierReturnResponse
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public SupplierReturnStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<SupplierReturnItemResponse> Items { get; set; } = new();
    }

    public class SupplierReturnItemResponse
    {
        public int Id { get; set; }
        public int PurchaseItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
