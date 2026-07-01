using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.Purchases
{
    public class PurchaseResponse
    {
        public int Id { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierBillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public PurchaseStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal GrandTotal { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<PurchaseItemResponse> Items { get; set; } = new();
    }

    public class PurchaseItemResponse
    {
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
