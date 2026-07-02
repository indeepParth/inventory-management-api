using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Statements.CustomerStatement
{
    public class Handler : IRequestHandler<Query, StatementResponse>
    {
        private readonly IPartyStatementRepository _statements;
        public Handler(IPartyStatementRepository statements) => _statements = statements;

        public async Task<StatementResponse> Handle(
            Query request, CancellationToken cancellationToken)
        {
            if (!await _statements.CustomerExistsAsync(
                    request.CustomerId, cancellationToken))
                throw new NotFoundException("Customer not found.");

            var toExclusive = request.DateTo.Date.AddDays(1);
            var invoices = await _statements.GetCustomerInvoicesThroughAsync(
                request.CustomerId, toExclusive, cancellationToken);
            var payments = await _statements.GetCustomerPaymentsThroughAsync(
                request.CustomerId, toExclusive, cancellationToken);

            var entries = invoices.Select(x => new StatementEntry
            {
                Type = StatementEntryType.Invoice,
                TransactionId = x.Id,
                TransactionDate = x.InvoiceDate,
                TimestampUtc = x.PostedAtUtc ?? x.CreatedAtUtc,
                ReferenceNumber = x.InvoiceNumber,
                Note = x.Notes,
                Amount = x.GrandTotal,
                BalanceChange = x.GrandTotal
            }).Concat(payments.Select(x => new StatementEntry
            {
                Type = x.ReversesPaymentId.HasValue
                    ? StatementEntryType.Reversal
                    : StatementEntryType.Payment,
                TransactionId = x.Id,
                TransactionDate = x.PaymentDate,
                TimestampUtc = x.CreatedAtUtc,
                ReferenceNumber = x.ReceiptNumber,
                ExternalReference = x.ExternalReference,
                Note = x.Note,
                Amount = x.Amount,
                BalanceChange = -x.Amount
            }));

            return StatementCalculator.Calculate(
                request.CustomerId, request.DateFrom, request.DateTo,
                request.PageNumber, request.PageSize, entries);
        }
    }
}
