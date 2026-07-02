using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Purchases.CancelPurchase
{
    public class Handler : IRequestHandler<Command, PurchaseResponse>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly ICurrentUserService _currentUserService;

        public Handler(
            IPurchaseRepository purchaseRepository,
            IStockMovementRepository stockMovementRepository,
            ICurrentUserService currentUserService)
        {
            _purchaseRepository = purchaseRepository;
            _stockMovementRepository = stockMovementRepository;
            _currentUserService = currentUserService;
        }

        public async Task<PurchaseResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            Purchase? cancelledPurchase = null;

            await _purchaseRepository.ExecuteInTransactionAsync(async transactionToken =>
            {
                var purchase = await _purchaseRepository.GetForUpdateAsync(
                    request.Id,
                    transactionToken) ?? throw new NotFoundException("Purchase not found.");

                if (purchase.Status == PurchaseStatus.Cancelled)
                {
                    cancelledPurchase = purchase;
                    return;
                }

                if (purchase.Status is PurchaseStatus.PartiallyPaid or PurchaseStatus.Paid)
                {
                    throw new BadRequestException(
                        "Purchase payments must be reversed before cancellation.");
                }

                if (purchase.Status == PurchaseStatus.Posted)
                {
                    await ReversePostedPurchaseAsync(purchase, transactionToken);
                }
                else if (purchase.Status != PurchaseStatus.Draft)
                {
                    throw new BadRequestException("Purchase cannot be cancelled.");
                }

                purchase.Status = PurchaseStatus.Cancelled;
                purchase.CancelledAtUtc = DateTime.UtcNow;
                await _purchaseRepository.SaveChangesAsync(transactionToken);
                cancelledPurchase = purchase;
            }, cancellationToken);

            return cancelledPurchase!.ToResponse();
        }

        private async Task ReversePostedPurchaseAsync(
            Purchase purchase,
            CancellationToken cancellationToken)
        {
            var movements =
                await _stockMovementRepository.GetPurchaseMovementsForUpdateAsync(
                    purchase.Id,
                    cancellationToken);

            if (movements.Count == 0)
            {
                throw new BadRequestException(
                    "Posted purchase movements could not be found.");
            }

            foreach (var productMovements in movements.GroupBy(x => x.ProductId))
            {
                if (productMovements.First().Product.Quantity <
                    productMovements.Sum(x => x.QuantityChange))
                {
                    throw new BadRequestException(
                        $"Insufficient stock to cancel purchase for product {productMovements.Key}.");
                }
            }

            var cancelledAtUtc = DateTime.UtcNow;

            foreach (var movement in movements)
            {
                var product = movement.Product;
                var balanceBefore = product.Quantity;
                var balanceAfter = balanceBefore - movement.QuantityChange;
                var inventoryValue =
                    (balanceBefore * product.AverageCost) -
                    (movement.QuantityChange * movement.UnitCost);

                product.Quantity = balanceAfter;
                product.AverageCost = balanceAfter == 0
                    ? 0
                    : RoundMoney(inventoryValue / balanceAfter);

                await _stockMovementRepository.AddAsync(new StockMovement
                {
                    ProductId = product.Id,
                    Product = product,
                    MovementType = StockMovementType.Reversal,
                    QuantityChange = -movement.QuantityChange,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    UnitCost = movement.UnitCost,
                    SourceType = "PurchaseCancellation",
                    SourceId = purchase.Id.ToString(),
                    Reference = purchase.PurchaseNumber,
                    OccurredAtUtc = cancelledAtUtc,
                    CreatedBy = _currentUserService.Username
                }, cancellationToken);
            }
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
