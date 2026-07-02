using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities
{
    public class Purchase
    {
        public int Id { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public string? SupplierBillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public PurchaseStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }
}
