using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public int? SalesInvoiceId { get; set; }
        public SalesInvoice? SalesInvoice { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ExternalReference { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int? ReversesPaymentId { get; set; }
        public Payment? ReversesPayment { get; set; }
        public Payment? Reversal { get; set; }
    }
}
