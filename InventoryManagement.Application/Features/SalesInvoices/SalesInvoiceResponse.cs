using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Features.SalesInvoices
{
    public class SalesInvoiceDetailResponse : SalesInvoiceResponse
    {
        public List<SalesInvoiceSourceChallanResponse> SourceChallans { get; set; } = new();
        public List<SalesInvoicePaymentResponse> Payments { get; set; } = new();
        public List<SalesInvoiceCustomerReturnResponse> CustomerReturns { get; set; } = new();
    }

    public class SalesInvoiceResponse
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public SalesInvoiceStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<SalesInvoiceItemResponse> Items { get; set; } = new();
    }

    public class SalesInvoiceItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal SellingUnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public decimal? CostAtSale { get; set; }
        public int? DeliveryChallanItemId { get; set; }
        public int? DeliveryChallanId { get; set; }
        public string? DeliveryChallanNumber { get; set; }
    }

    public class SalesInvoiceSourceChallanResponse
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
    }

    public class SalesInvoicePaymentResponse
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
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

    public class SalesInvoiceCustomerReturnResponse
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public CustomerReturnStatus Status { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
