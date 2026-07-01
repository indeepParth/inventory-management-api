using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.PostSalesInvoice
{
    public class Handler : IRequestHandler<Command, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _invoiceRepository;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly ICurrentUserService _currentUserService;

        public Handler(
            ISalesInvoiceRepository invoiceRepository,
            IStockMovementRepository stockMovementRepository,
            ICurrentUserService currentUserService)
        {
            _invoiceRepository = invoiceRepository;
            _stockMovementRepository = stockMovementRepository;
            _currentUserService = currentUserService;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            SalesInvoice? postedInvoice = null;

            await _invoiceRepository.ExecuteInTransactionAsync(
                async transactionToken =>
                {
                    var invoice = await _invoiceRepository.GetForUpdateAsync(
                        request.Id,
                        transactionToken) ??
                        throw new NotFoundException("Sales invoice not found.");

                    if (invoice.Status == SalesInvoiceStatus.Posted)
                    {
                        postedInvoice = invoice;
                        return;
                    }

                    if (invoice.Status != SalesInvoiceStatus.Draft)
                    {
                        throw new BadRequestException(
                            "Only Draft sales invoices may be posted.");
                    }

                    var hasChallanItems =
                        invoice.Items.Any(x => x.DeliveryChallanItemId.HasValue);
                    var hasDirectItems =
                        invoice.Items.Any(x => !x.DeliveryChallanItemId.HasValue);
                    if (hasChallanItems && hasDirectItems)
                    {
                        throw new BadRequestException(
                            "Direct and delivery challan items cannot be mixed.");
                    }

                    if (!hasChallanItems)
                    {
                        foreach (var group in invoice.Items.GroupBy(x => x.ProductId))
                        {
                            var requiredQuantity = group.Sum(x => x.Quantity);
                            if (group.First().Product.Quantity < requiredQuantity)
                            {
                                throw new BadRequestException(
                                    $"Insufficient stock for product {group.Key}.");
                            }
                        }
                    }

                    var postedAtUtc = DateTime.UtcNow;
                    if (hasChallanItems)
                    {
                        foreach (var item in invoice.Items)
                        {
                            var source = item.DeliveryChallanItem!;
                            var cost = await _stockMovementRepository
                                .GetDeliveryChallanItemCostAsync(
                                    source.DeliveryChallanId,
                                    item.ProductId,
                                    transactionToken);
                            if (!cost.HasValue)
                            {
                                throw new BadRequestException(
                                    $"Original stock movement was not found for delivery challan item {source.Id}.");
                            }
                            item.CostAtSale = cost.Value;
                        }

                        var challans = await _invoiceRepository
                            .GetLinkedChallansForUpdateAsync(
                                invoice.Id,
                                transactionToken);
                        foreach (var challan in challans.Where(x =>
                            x.Items.All(item => item.SalesInvoiceItems.Any(link =>
                                link.SalesInvoiceId == invoice.Id ||
                                link.SalesInvoice.Status ==
                                    SalesInvoiceStatus.Posted))))
                        {
                            challan.Status = DeliveryChallanStatus.Invoiced;
                            challan.InvoicedAtUtc = postedAtUtc;
                            challan.UpdatedAtUtc = postedAtUtc;
                        }
                    }
                    else
                    {
                        foreach (var item in invoice.Items)
                        {
                            var product = item.Product;
                            var balanceBefore = product.Quantity;
                            var costAtSale = product.AverageCost;
                            product.Quantity -= item.Quantity;
                            item.CostAtSale = costAtSale;

                            await _stockMovementRepository.AddAsync(
                                new StockMovement
                                {
                                    ProductId = product.Id,
                                    Product = product,
                                    MovementType = StockMovementType.Sale,
                                    QuantityChange = -item.Quantity,
                                    BalanceBefore = balanceBefore,
                                    BalanceAfter = product.Quantity,
                                    UnitCost = costAtSale,
                                    SourceType = "SalesInvoice",
                                    SourceId = invoice.Id.ToString(),
                                    Reference = invoice.InvoiceNumber,
                                    OccurredAtUtc = postedAtUtc,
                                    CreatedBy = _currentUserService.Username
                                },
                                transactionToken);
                        }
                    }

                    invoice.Customer.BalanceDue = invoice.GrandTotal;
                    invoice.Customer.UpdatedAtUtc = postedAtUtc;
                    invoice.Status = SalesInvoiceStatus.Posted;
                    invoice.PostedAtUtc = postedAtUtc;
                    invoice.UpdatedAtUtc = postedAtUtc;
                    await _invoiceRepository.SaveChangesAsync(transactionToken);
                    postedInvoice = invoice;
                },
                cancellationToken);

            return postedInvoice!.ToResponse();
        }
    }
}
