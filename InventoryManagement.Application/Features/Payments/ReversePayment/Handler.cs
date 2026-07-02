using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Payments.ReversePayment
{
    public class Handler : IRequestHandler<Command, PaymentResponse>
    {
        private readonly IPaymentRepository _payments;
        private readonly ICurrentUserService _currentUser;

        public Handler(IPaymentRepository payments, ICurrentUserService currentUser)
        {
            _payments = payments;
            _currentUser = currentUser;
        }

        public async Task<PaymentResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            Payment? reversal = null;
            await _payments.ExecuteInTransactionAsync(async transactionToken =>
            {
                var original = await _payments.GetForReversalAsync(
                    request.Id, transactionToken) ??
                    throw new NotFoundException("Payment not found.");
                if (original.ReversesPaymentId.HasValue)
                    throw new BadRequestException("A reversal cannot be reversed.");
                if (original.Reversal != null)
                    throw new BadRequestException("Payment has already been reversed.");

                var receiptNumber = request.ReceiptNumber.Trim();
                if (await _payments.ReceiptNumberExistsAsync(receiptNumber, transactionToken))
                    throw new BadRequestException("Receipt number already exists.");

                var now = DateTime.UtcNow;
                reversal = new Payment
                {
                    ReceiptNumber = receiptNumber,
                    CustomerId = original.CustomerId,
                    Customer = original.Customer,
                    SalesInvoiceId = original.SalesInvoiceId,
                    SalesInvoice = original.SalesInvoice,
                    PaymentDate = request.PaymentDate,
                    Amount = -original.Amount,
                    Method = original.Method,
                    ExternalReference = Normalize(request.ExternalReference),
                    Note = Normalize(request.Note),
                    CreatedAtUtc = now,
                    CreatedBy = _currentUser.Username,
                    ReversesPaymentId = original.Id,
                    ReversesPayment = original
                };
                await _payments.AddAsync(reversal, transactionToken);

                original.Customer.BalanceDue += original.Amount;
                original.Customer.UpdatedAtUtc = now;
                if (original.SalesInvoice != null)
                {
                    original.SalesInvoice.AmountPaid -= original.Amount;
                    original.SalesInvoice.BalanceDue += original.Amount;
                    original.SalesInvoice.Status =
                        original.SalesInvoice.AmountPaid == 0
                            ? SalesInvoiceStatus.Posted
                            : SalesInvoiceStatus.PartiallyPaid;
                    original.SalesInvoice.UpdatedAtUtc = now;
                }
                await _payments.SaveChangesAsync(transactionToken);
            }, cancellationToken);

            return reversal!.ToResponse();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
