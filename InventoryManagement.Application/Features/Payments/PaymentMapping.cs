using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.Payments
{
    public static class PaymentMapping
    {
        public static PaymentResponse ToResponse(this Payment payment) => new()
        {
            Id = payment.Id,
            ReceiptNumber = payment.ReceiptNumber,
            CustomerId = payment.CustomerId,
            CustomerName = payment.Customer.Name,
            SalesInvoiceId = payment.SalesInvoiceId,
            InvoiceNumber = payment.SalesInvoice?.InvoiceNumber,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Method = payment.Method,
            ExternalReference = payment.ExternalReference,
            Note = payment.Note,
            CreatedAtUtc = payment.CreatedAtUtc,
            CreatedBy = payment.CreatedBy,
            ReversesPaymentId = payment.ReversesPaymentId,
            ReversalPaymentId = payment.Reversal?.Id
        };
    }
}
