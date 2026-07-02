using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.CreateSupplierReturn
{
    public class Handler : IRequestHandler<Command, SupplierReturnResponse>
    {
        private readonly ISupplierReturnRepository _returns;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            ISupplierReturnRepository returns,
            ICurrentUserService currentUser)
        {
            _returns = returns;
            _currentUser = currentUser;
        }

        public async Task<SupplierReturnResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var returnNumber = request.ReturnNumber.Trim();
            if (await _returns.ReturnNumberExistsAsync(
                returnNumber,
                cancellationToken))
                throw new BadRequestException("Return number already exists.");

            if (request.Items.GroupBy(x => x.PurchaseItemId)
                .Any(x => x.Count() > 1))
                throw new BadRequestException(
                    "Each purchase item may appear only once in a return.");

            var purchase = await _returns.GetPurchaseForReturnAsync(
                request.PurchaseId,
                cancellationToken) ??
                throw new NotFoundException("Purchase not found.");
            if (purchase.Status is PurchaseStatus.Draft or PurchaseStatus.Cancelled)
                throw new BadRequestException(
                    "Returns may reference only posted purchases.");

            var itemIds = request.Items.Select(x => x.PurchaseItemId).ToList();
            var purchaseItems = purchase.Items
                .Where(x => itemIds.Contains(x.Id))
                .ToDictionary(x => x.Id);
            if (purchaseItems.Count != itemIds.Count)
                throw new BadRequestException(
                    "Every return item must belong to the referenced purchase.");

            var returned = await _returns.GetPostedReturnedQuantitiesAsync(
                itemIds,
                null,
                cancellationToken);
            var now = DateTime.UtcNow;
            var supplierReturn = new SupplierReturn
            {
                ReturnNumber = returnNumber,
                PurchaseId = purchase.Id,
                Purchase = purchase,
                SupplierId = purchase.SupplierId,
                Supplier = purchase.Supplier,
                ReturnDate = request.ReturnDate,
                Status = SupplierReturnStatus.Draft,
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? null
                    : request.Notes.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBy = _currentUser.Username
            };

            foreach (var input in request.Items)
            {
                var source = purchaseItems[input.PurchaseItemId];
                var alreadyReturned = returned.GetValueOrDefault(source.Id);
                if (input.Quantity > source.Quantity - alreadyReturned)
                    throw new BadRequestException(
                        $"Return quantity exceeds the remaining returnable quantity for purchase item {source.Id}.");

                var subtotal = RoundMoney(input.Quantity * source.UnitCost);
                var tax = RoundMoney(subtotal * source.TaxRate / 100m);
                supplierReturn.Items.Add(new SupplierReturnItem
                {
                    PurchaseItemId = source.Id,
                    PurchaseItem = source,
                    ProductId = source.ProductId,
                    Product = source.Product,
                    Quantity = input.Quantity,
                    UnitCost = source.UnitCost,
                    TaxRate = source.TaxRate,
                    TaxAmount = tax,
                    LineTotal = subtotal + tax
                });
                supplierReturn.Subtotal += subtotal;
                supplierReturn.TaxAmount += tax;
            }

            supplierReturn.GrandTotal =
                supplierReturn.Subtotal + supplierReturn.TaxAmount;
            await _returns.AddAsync(supplierReturn, cancellationToken);
            await _returns.SaveChangesAsync(cancellationToken);
            return supplierReturn.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
