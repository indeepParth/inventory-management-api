using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Persistence;
using MediatR;

namespace InventoryManagement.Application.Features.Statements.SupplierStatement
{
    public class Handler : IRequestHandler<Query, StatementResponse>
    {
        private readonly IPartyStatementRepository _statements;
        public Handler(IPartyStatementRepository statements) => _statements = statements;

        public async Task<StatementResponse> Handle(
            Query request, CancellationToken cancellationToken)
        {
            if (!await _statements.SupplierExistsAsync(
                    request.SupplierId, cancellationToken))
                throw new NotFoundException("Supplier not found.");

            var toExclusive = request.DateTo.Date.AddDays(1);
            var purchases = await _statements.GetSupplierPurchasesThroughAsync(
                request.SupplierId, toExclusive, cancellationToken);
            var payments = await _statements.GetSupplierPaymentsThroughAsync(
                request.SupplierId, toExclusive, cancellationToken);

            var entries = purchases.Select(x => new StatementEntry
            {
                Type = StatementEntryType.Purchase,
                TransactionId = x.Id,
                TransactionDate = x.BillDate,
                TimestampUtc = x.PostedAtUtc ?? x.CreatedAtUtc,
                ReferenceNumber = x.PurchaseNumber,
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
                request.SupplierId, request.DateFrom, request.DateTo,
                request.PageNumber, request.PageSize, entries);
        }
    }
}
