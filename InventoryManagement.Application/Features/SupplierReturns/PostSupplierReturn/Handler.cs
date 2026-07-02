using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.PostSupplierReturn
{
    public class Handler : IRequestHandler<Command, SupplierReturnResponse>
    {
        private readonly ISupplierReturnRepository _returns;
        private readonly IStockMovementRepository _movements;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            ISupplierReturnRepository returns,
            IStockMovementRepository movements,
            ICurrentUserService currentUser)
        {
            _returns = returns;
            _movements = movements;
            _currentUser = currentUser;
        }

        public async Task<SupplierReturnResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            SupplierReturn? posted = null;
            await _returns.ExecuteInTransactionAsync(async transactionToken =>
            {
                var supplierReturn = await _returns.GetForUpdateAsync(
                    request.Id,
                    transactionToken) ??
                    throw new NotFoundException("Supplier return not found.");
                if (supplierReturn.Status == SupplierReturnStatus.Posted)
                {
                    posted = supplierReturn;
                    return;
                }
                if (supplierReturn.Status != SupplierReturnStatus.Draft)
                    throw new BadRequestException(
                        "Only Draft supplier returns may be posted.");
                if (supplierReturn.Purchase.Status is
                    PurchaseStatus.Draft or PurchaseStatus.Cancelled)
                    throw new BadRequestException(
                        "The referenced purchase is no longer posted.");

                var itemIds = supplierReturn.Items
                    .Select(x => x.PurchaseItemId)
                    .ToList();
                var returned = await _returns.GetPostedReturnedQuantitiesAsync(
                    itemIds,
                    supplierReturn.Id,
                    transactionToken);
                foreach (var item in supplierReturn.Items)
                {
                    var remaining =
                        item.PurchaseItem.Quantity -
                        returned.GetValueOrDefault(item.PurchaseItemId);
                    if (item.Quantity > remaining)
                        throw new BadRequestException(
                            $"Return quantity exceeds the remaining returnable quantity for purchase item {item.PurchaseItemId}.");
                }

                foreach (var group in supplierReturn.Items.GroupBy(x =>
                    x.ProductId))
                {
                    var product = group.First().Product;
                    var quantity = group.Sum(x => x.Quantity);
                    var returnedValue = group.Sum(x => x.Quantity * x.UnitCost);
                    if (product.Quantity < quantity)
                        throw new BadRequestException(
                            $"Insufficient stock for product {product.Id}.");
                    var remainingValue =
                        product.Quantity * product.AverageCost - returnedValue;
                    if (remainingValue < 0)
                        throw new BadRequestException(
                            $"Supplier return would create negative inventory value for product {product.Id}.");
                }

                var now = DateTime.UtcNow;
                foreach (var group in supplierReturn.Items.GroupBy(x =>
                    x.ProductId))
                {
                    var product = group.First().Product;
                    var startingQuantity = product.Quantity;
                    var startingValue =
                        startingQuantity * product.AverageCost;
                    var runningQuantity = startingQuantity;
                    foreach (var item in group)
                    {
                        var before = runningQuantity;
                        runningQuantity -= item.Quantity;
                        product.Quantity = runningQuantity;
                        await _movements.AddAsync(new StockMovement
                        {
                            ProductId = product.Id,
                            Product = product,
                            MovementType = StockMovementType.SupplierReturn,
                            QuantityChange = -item.Quantity,
                            BalanceBefore = before,
                            BalanceAfter = runningQuantity,
                            UnitCost = item.UnitCost,
                            SourceType = "SupplierReturn",
                            SourceId = supplierReturn.Id.ToString(),
                            Reference = supplierReturn.ReturnNumber,
                            OccurredAtUtc = now,
                            CreatedBy = _currentUser.Username
                        }, transactionToken);
                    }

                    var returnedValue = group.Sum(x =>
                        x.Quantity * x.UnitCost);
                    product.AverageCost = runningQuantity == 0
                        ? 0
                        : RoundMoney(
                            (startingValue - returnedValue) /
                            runningQuantity);
                }

                supplierReturn.Purchase.BalanceDue -=
                    supplierReturn.GrandTotal;
                supplierReturn.Status = SupplierReturnStatus.Posted;
                supplierReturn.PostedAtUtc = now;
                supplierReturn.UpdatedAtUtc = now;
                await _returns.SaveChangesAsync(transactionToken);
                posted = supplierReturn;
            }, cancellationToken);
            return posted!.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
