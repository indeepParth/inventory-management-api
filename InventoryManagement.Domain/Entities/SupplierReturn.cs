using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities
{
    public class SupplierReturn
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int PurchaseId { get; set; }
        public Purchase Purchase { get; set; } = null!;
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
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
        public ICollection<SupplierReturnItem> Items { get; set; } =
            new List<SupplierReturnItem>();
    }
}
