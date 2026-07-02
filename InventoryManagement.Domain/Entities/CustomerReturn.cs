using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities
{
    public class CustomerReturn
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int SalesInvoiceId { get; set; }
        public SalesInvoice SalesInvoice { get; set; } = null!;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
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
        public ICollection<CustomerReturnItem> Items { get; set; } =
            new List<CustomerReturnItem>();
    }
}
