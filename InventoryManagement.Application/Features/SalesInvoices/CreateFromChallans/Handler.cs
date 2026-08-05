using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.Products;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.CreateFromChallans
{
    public class Handler : IRequestHandler<Command, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _invoices;
        private readonly IStockMovementRepository _stockMovements;
        private readonly ICurrentUserService _currentUser;
        private readonly IDocumentNumberService _documentNumbers;

        public Handler(
            ISalesInvoiceRepository invoices,
            IStockMovementRepository stockMovements,
            ICurrentUserService currentUser,
            IDocumentNumberService documentNumbers)
        {
            _invoices = invoices;
            _stockMovements = stockMovements;
            _currentUser = currentUser;
            _documentNumbers = documentNumbers;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            SalesInvoice? created = null;
            await _invoices.ExecuteInTransactionAsync(async transactionToken =>
            {
                var invoiceNumber = await _documentNumbers.GenerateAsync(
                    DocumentNumberType.SalesInvoice,
                    request.InvoiceDate,
                    cancellationToken: transactionToken);

                var ids = request.Items.Select(x => x.DeliveryChallanItemId).ToList();
                if (ids.Distinct().Count() != ids.Count)
                {
                    throw new BadRequestException(
                        "A delivery challan item can only be selected once.");
                }

                var challanItems = await _invoices.GetChallanItemsForInvoiceAsync(
                    ids, transactionToken);
                if (challanItems.Count != ids.Count)
                {
                    throw new NotFoundException("One or more delivery challan items were not found.");
                }

                if (challanItems.Any(x =>
                    x.DeliveryChallan.Status != DeliveryChallanStatus.Posted))
                {
                    throw new BadRequestException(
                        "Only Posted delivery challans may be invoiced.");
                }

                if (challanItems.Any(x =>
                    x.SalesInvoiceItems.Any(link =>
                        link.IsChallanAllocationActive)))
                {
                    throw new BadRequestException(
                        "One or more delivery challan items have already been invoiced.");
                }

                var customerId = challanItems[0].DeliveryChallan.CustomerId;
                if (challanItems.Any(x =>
                    x.DeliveryChallan.CustomerId != customerId))
                {
                    throw new BadRequestException(
                        "All delivery challans must belong to the same customer.");
                }

                var deliveryChargeTotal = RoundMoney(challanItems
                    .Select(x => x.DeliveryChallan)
                    .DistinctBy(x => x.Id)
                    .Sum(x => x.DeliveryCharge));
                var otherCharges = deliveryChargeTotal > 0
                    ? deliveryChargeTotal
                    : request.OtherCharges;

                var now = DateTime.UtcNow;
                var invoice = new SalesInvoice
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = customerId,
                    Customer = challanItems[0].DeliveryChallan.Customer,
                    InvoiceDate = request.InvoiceDate,
                    Status = SalesInvoiceStatus.Draft,
                    Discount = request.Discount,
                    OtherCharges = otherCharges,
                    Notes = string.IsNullOrWhiteSpace(request.Notes)
                        ? null : request.Notes.Trim(),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBy = _currentUser.Username
                };

                foreach (var input in request.Items)
                {
                    var source = challanItems.Single(x =>
                        x.Id == input.DeliveryChallanItemId);
                    var lineSubtotal = RoundMoney(
                        source.EnteredQuantity * input.SellingUnitPrice);
                    var tax = RoundMoney(lineSubtotal * input.TaxRate / 100m);
                    var stockProduct = ProductStock.GetStockProduct(source.Product);
                    var cost = await _stockMovements.GetDeliveryChallanItemCostAsync(
                        source.DeliveryChallanId,
                        stockProduct.Id,
                        transactionToken);
                    if (!cost.HasValue)
                    {
                        throw new BadRequestException(
                            $"Original stock movement was not found for delivery challan item {source.Id}.");
                    }

                    var factor = ProductStock.IsSubProduct(source.Product)
                        ? source.Product.FactorToBaseProduct!.Value
                        : 1m;

                    invoice.Items.Add(new SalesInvoiceItem
                    {
                        ProductId = source.ProductId,
                        Product = source.Product,
                        Quantity = source.EnteredQuantity,
                        SellingUnitPrice = input.SellingUnitPrice,
                        TaxRate = input.TaxRate,
                        TaxAmount = tax,
                        LineTotal = lineSubtotal + tax,
                        CostAtSale = cost.Value * factor,
                        DeliveryChallanItemId = source.Id,
                        DeliveryChallanItem = source,
                        IsChallanAllocationActive = true
                    });
                    invoice.Subtotal += lineSubtotal;
                    invoice.TaxAmount += tax;
                }

                invoice.GrandTotal = RoundMoney(
                    invoice.Subtotal - invoice.Discount +
                    invoice.TaxAmount + invoice.OtherCharges);
                if (invoice.GrandTotal < 0)
                    throw new BadRequestException("Grand total cannot be negative.");
                invoice.BalanceDue = invoice.GrandTotal;
                invoice.AmountPaid = 0;
                await _invoices.AddAsync(invoice, transactionToken);
                await _invoices.SaveChangesAsync(transactionToken);

                var postedAtUtc = DateTime.UtcNow;
                var challans = await _invoices.GetLinkedChallansForUpdateAsync(
                    invoice.Id,
                    transactionToken);
                foreach (var challan in challans.Where(x =>
                    x.Items.All(item => item.SalesInvoiceItems.Any(link =>
                        link.IsChallanAllocationActive &&
                        (link.SalesInvoiceId == invoice.Id ||
                         link.SalesInvoice.Status == SalesInvoiceStatus.Posted)))))
                {
                    challan.Status = DeliveryChallanStatus.Invoiced;
                    challan.InvoicedAtUtc = postedAtUtc;
                    challan.UpdatedAtUtc = postedAtUtc;
                }

                invoice.Customer.BalanceDue += invoice.GrandTotal;
                invoice.Customer.UpdatedAtUtc = postedAtUtc;
                invoice.Status = SalesInvoiceStatus.Posted;
                invoice.PostedAtUtc = postedAtUtc;
                invoice.UpdatedAtUtc = postedAtUtc;
                await _invoices.SaveChangesAsync(transactionToken);
                created = invoice;
            }, cancellationToken);

            return created!.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
