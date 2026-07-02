using MediatR;

namespace InventoryManagement.Application.Features.Payments.ReversePayment
{
    public record Command : IRequest<PaymentResponse>
    {
        public int Id { get; init; }
        public string ReceiptNumber { get; init; } = string.Empty;
        public DateTime PaymentDate { get; init; }
        public string? ExternalReference { get; init; }
        public string? Note { get; init; }
    }
}
