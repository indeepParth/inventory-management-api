using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.Payments
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? SalesInvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ExternalReference { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int? ReversesPaymentId { get; set; }
        public int? ReversalPaymentId { get; set; }
    }
}
