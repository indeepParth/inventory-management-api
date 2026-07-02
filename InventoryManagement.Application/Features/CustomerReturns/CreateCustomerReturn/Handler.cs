using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.CustomerReturns.CreateCustomerReturn
{
    public class Handler : IRequestHandler<Command, CustomerReturnResponse>
    {
        private readonly ICustomerReturnRepository _returns;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            ICustomerReturnRepository returns,
            ICurrentUserService currentUser)
        {
            _returns = returns;
            _currentUser = currentUser;
        }

        public async Task<CustomerReturnResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var returnNumber = request.ReturnNumber.Trim();
            if (await _returns.ReturnNumberExistsAsync(
                returnNumber,
                cancellationToken))
                throw new BadRequestException("Return number already exists.");

            if (request.Items.GroupBy(x => x.SalesInvoiceItemId)
                .Any(x => x.Count() > 1))
                throw new BadRequestException(
                    "Each invoice item may appear only once in a return.");

            var invoice = await _returns.GetInvoiceForReturnAsync(
                request.SalesInvoiceId,
                cancellationToken) ??
                throw new NotFoundException("Sales invoice not found.");
            if (invoice.Status is SalesInvoiceStatus.Draft or
                SalesInvoiceStatus.Cancelled)
                throw new BadRequestException(
                    "Returns may reference only posted sales invoices.");

            var itemIds = request.Items
                .Select(x => x.SalesInvoiceItemId)
                .ToList();
            var invoiceItems = invoice.Items
                .Where(x => itemIds.Contains(x.Id))
                .ToDictionary(x => x.Id);
            if (invoiceItems.Count != itemIds.Count)
                throw new BadRequestException(
                    "Every return item must belong to the referenced invoice.");

            var returned = await _returns.GetPostedReturnedQuantitiesAsync(
                itemIds,
                null,
                cancellationToken);
            var now = DateTime.UtcNow;
            var customerReturn = new CustomerReturn
            {
                ReturnNumber = returnNumber,
                SalesInvoiceId = invoice.Id,
                SalesInvoice = invoice,
                CustomerId = invoice.CustomerId,
                Customer = invoice.Customer,
                ReturnDate = request.ReturnDate,
                Status = CustomerReturnStatus.Draft,
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? null
                    : request.Notes.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBy = _currentUser.Username
            };

            foreach (var input in request.Items)
            {
                var source = invoiceItems[input.SalesInvoiceItemId];
                if (!source.CostAtSale.HasValue)
                    throw new BadRequestException(
                        $"Invoice item {source.Id} has no posted cost history.");

                var alreadyReturned = returned.GetValueOrDefault(source.Id);
                if (input.Quantity > source.Quantity - alreadyReturned)
                    throw new BadRequestException(
                        $"Return quantity exceeds the remaining returnable quantity for invoice item {source.Id}.");

                var subtotal = RoundMoney(
                    input.Quantity * source.SellingUnitPrice);
                var tax = RoundMoney(subtotal * source.TaxRate / 100m);
                customerReturn.Items.Add(new CustomerReturnItem
                {
                    SalesInvoiceItemId = source.Id,
                    SalesInvoiceItem = source,
                    ProductId = source.ProductId,
                    Product = source.Product,
                    Quantity = input.Quantity,
                    SellingUnitPrice = source.SellingUnitPrice,
                    TaxRate = source.TaxRate,
                    TaxAmount = tax,
                    LineTotal = subtotal + tax,
                    CostAtSale = source.CostAtSale.Value
                });
                customerReturn.Subtotal += subtotal;
                customerReturn.TaxAmount += tax;
            }

            customerReturn.GrandTotal =
                customerReturn.Subtotal + customerReturn.TaxAmount;
            await _returns.AddAsync(customerReturn, cancellationToken);
            await _returns.SaveChangesAsync(cancellationToken);
            return customerReturn.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
