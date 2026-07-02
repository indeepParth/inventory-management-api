using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.SupplierReturns.CancelSupplierReturn
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
            SupplierReturn? cancelled = null;
            await _returns.ExecuteInTransactionAsync(async transactionToken =>
            {
                var supplierReturn = await _returns.GetForUpdateAsync(
                    request.Id,
                    transactionToken) ??
                    throw new NotFoundException("Supplier return not found.");
                if (supplierReturn.Status == SupplierReturnStatus.Cancelled)
                {
                    cancelled = supplierReturn;
                    return;
                }

                var now = DateTime.UtcNow;
                if (supplierReturn.Status == SupplierReturnStatus.Posted)
                {
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
                            runningQuantity += item.Quantity;
                            product.Quantity = runningQuantity;
                            await _movements.AddAsync(new StockMovement
                            {
                                ProductId = product.Id,
                                Product = product,
                                MovementType = StockMovementType.Reversal,
                                QuantityChange = item.Quantity,
                                BalanceBefore = before,
                                BalanceAfter = runningQuantity,
                                UnitCost = item.UnitCost,
                                SourceType = "SupplierReturnCancellation",
                                SourceId = supplierReturn.Id.ToString(),
                                Reference = supplierReturn.ReturnNumber,
                                OccurredAtUtc = now,
                                CreatedBy = _currentUser.Username
                            }, transactionToken);
                        }

                        var restoredValue = group.Sum(x =>
                            x.Quantity * x.UnitCost);
                        product.AverageCost = RoundMoney(
                            (startingValue + restoredValue) /
                            runningQuantity);
                    }
                    supplierReturn.Purchase.BalanceDue +=
                        supplierReturn.GrandTotal;
                }
                else if (supplierReturn.Status != SupplierReturnStatus.Draft)
                    throw new BadRequestException(
                        "Supplier return cannot be cancelled.");

                supplierReturn.Status = SupplierReturnStatus.Cancelled;
                supplierReturn.CancelledAtUtc = now;
                supplierReturn.UpdatedAtUtc = now;
                await _returns.SaveChangesAsync(transactionToken);
                cancelled = supplierReturn;
            }, cancellationToken);
            return cancelled!.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
