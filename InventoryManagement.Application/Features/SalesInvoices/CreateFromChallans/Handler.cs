using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SalesInvoices.CreateFromChallans
{
    public class Handler : IRequestHandler<Command, SalesInvoiceResponse>
    {
        private readonly ISalesInvoiceRepository _invoices;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            ISalesInvoiceRepository invoices,
            ICurrentUserService currentUser)
        {
            _invoices = invoices;
            _currentUser = currentUser;
        }

        public async Task<SalesInvoiceResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            SalesInvoice? created = null;
            await _invoices.ExecuteInTransactionAsync(async transactionToken =>
            {
                var invoiceNumber = request.InvoiceNumber.Trim();
                if (await _invoices.InvoiceNumberExistsAsync(
                    invoiceNumber, transactionToken))
                {
                    throw new BadRequestException("Invoice number already exists.");
                }

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

                if (challanItems.Any(x => x.SalesInvoiceItems.Count != 0))
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

                var now = DateTime.UtcNow;
                var invoice = new SalesInvoice
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = customerId,
                    Customer = challanItems[0].DeliveryChallan.Customer,
                    InvoiceDate = request.InvoiceDate,
                    Status = SalesInvoiceStatus.Draft,
                    Discount = request.Discount,
                    OtherCharges = request.OtherCharges,
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
                        source.Quantity * input.SellingUnitPrice);
                    var tax = RoundMoney(lineSubtotal * input.TaxRate / 100m);
                    invoice.Items.Add(new SalesInvoiceItem
                    {
                        ProductId = source.ProductId,
                        Product = source.Product,
                        Quantity = source.Quantity,
                        SellingUnitPrice = input.SellingUnitPrice,
                        TaxRate = input.TaxRate,
                        TaxAmount = tax,
                        LineTotal = lineSubtotal + tax,
                        DeliveryChallanItemId = source.Id,
                        DeliveryChallanItem = source
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
                await _invoices.AddAsync(invoice, transactionToken);
                await _invoices.SaveChangesAsync(transactionToken);
                created = invoice;
            }, cancellationToken);

            return created!.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
