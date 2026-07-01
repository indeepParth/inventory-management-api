using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.CancelSalesInvoice
{
    public class Handler : IRequestHandler<Command, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _invoices;
        private readonly IStockMovementRepository _movements;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            ISalesInvoiceRepository invoices,
            IStockMovementRepository movements,
            ICurrentUserService currentUser)
        {
            _invoices = invoices;
            _movements = movements;
            _currentUser = currentUser;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            SalesInvoice? cancelled = null;
            await _invoices.ExecuteInTransactionAsync(async transactionToken =>
            {
                var invoice = await _invoices.GetForUpdateAsync(
                    request.Id,
                    transactionToken) ??
                    throw new NotFoundException("Sales invoice not found.");

                if (invoice.Status == SalesInvoiceStatus.Cancelled)
                {
                    cancelled = invoice;
                    return;
                }

                if (invoice.Status is SalesInvoiceStatus.Paid or
                    SalesInvoiceStatus.PartiallyPaid)
                {
                    throw new BadRequestException(
                        "Invoice payments must be reversed before cancellation.");
                }

                var cancelledAtUtc = DateTime.UtcNow;
                if (invoice.Status == SalesInvoiceStatus.Posted)
                {
                    var hasChallanItems = invoice.Items.Any(x =>
                        x.DeliveryChallanItemId.HasValue);
                    if (hasChallanItems)
                        await CancelChallanInvoiceAsync(
                            invoice, cancelledAtUtc, transactionToken);
                    else
                        await ReverseDirectInvoiceAsync(
                            invoice, cancelledAtUtc, transactionToken);

                    invoice.Customer.BalanceDue = Math.Max(
                        0,
                        invoice.Customer.BalanceDue - invoice.GrandTotal);
                    invoice.Customer.UpdatedAtUtc = cancelledAtUtc;
                }
                else if (invoice.Status != SalesInvoiceStatus.Draft)
                {
                    throw new BadRequestException(
                        "Sales invoice cannot be cancelled.");
                }

                invoice.Status = SalesInvoiceStatus.Cancelled;
                invoice.CancelledAtUtc = cancelledAtUtc;
                invoice.UpdatedAtUtc = cancelledAtUtc;
                await _invoices.SaveChangesAsync(transactionToken);
                cancelled = invoice;
            }, cancellationToken);

            return cancelled!.ToResponse();
        }

        private async Task ReverseDirectInvoiceAsync(
            SalesInvoice invoice,
            DateTime cancelledAtUtc,
            CancellationToken cancellationToken)
        {
            var originalMovements =
                await _movements.GetSalesInvoiceMovementsForUpdateAsync(
                    invoice.Id,
                    cancellationToken);
            if (originalMovements.Count == 0)
                throw new BadRequestException(
                    "Posted sales invoice movements could not be found.");

            foreach (var movement in originalMovements)
            {
                var product = movement.Product;
                var balanceBefore = product.Quantity;
                var quantityRestored = -movement.QuantityChange;
                product.Quantity += quantityRestored;

                await _movements.AddAsync(new StockMovement
                {
                    ProductId = product.Id,
                    Product = product,
                    MovementType = StockMovementType.Reversal,
                    QuantityChange = quantityRestored,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = product.Quantity,
                    UnitCost = movement.UnitCost,
                    SourceType = "SalesInvoiceCancellation",
                    SourceId = invoice.Id.ToString(),
                    Reference = invoice.InvoiceNumber,
                    OccurredAtUtc = cancelledAtUtc,
                    CreatedBy = _currentUser.Username
                }, cancellationToken);
            }
        }

        private async Task CancelChallanInvoiceAsync(
            SalesInvoice invoice,
            DateTime cancelledAtUtc,
            CancellationToken cancellationToken)
        {
            var challans = await _invoices.GetLinkedChallansForUpdateAsync(
                invoice.Id,
                cancellationToken);
            foreach (var item in invoice.Items)
                item.IsChallanAllocationActive = false;

            foreach (var challan in challans)
            {
                challan.Status = DeliveryChallanStatus.Posted;
                challan.InvoicedAtUtc = null;
                challan.UpdatedAtUtc = cancelledAtUtc;
            }
        }
    }
}
