using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Payments.CreatePayment
{
    public record Command : IRequest<PaymentResponse>
    {
        public string ReceiptNumber { get; init; } = string.Empty;
        public int CustomerId { get; init; }
        public int? SalesInvoiceId { get; init; }
        public DateTime PaymentDate { get; init; }
        public decimal Amount { get; init; }
        public PaymentMethod Method { get; init; }
        public string? ExternalReference { get; init; }
        public string? Note { get; init; }
    }
}
