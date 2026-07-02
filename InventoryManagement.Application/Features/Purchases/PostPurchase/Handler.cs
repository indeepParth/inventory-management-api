using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.Purchases.PostPurchase
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
            Purchase? postedPurchase = null;

            await _purchaseRepository.ExecuteInTransactionAsync(async transactionToken =>
            {
                var purchase = await _purchaseRepository.GetForUpdateAsync(
                    request.Id,
                    transactionToken) ?? throw new NotFoundException("Purchase not found.");

                if (purchase.Status != PurchaseStatus.Draft)
                {
                    throw new BadRequestException("Only Draft purchases may be posted.");
                }

                var postedAtUtc = DateTime.UtcNow;

                foreach (var item in purchase.Items)
                {
                    var product = item.Product;
                    var balanceBefore = product.Quantity;
                    var balanceAfter = balanceBefore + item.Quantity;
                    var weightedCost =
                        ((balanceBefore * product.AverageCost) +
                         (item.Quantity * item.UnitCost)) /
                        balanceAfter;

                    product.Quantity = balanceAfter;
                    product.AverageCost = RoundMoney(weightedCost);

                    await _stockMovementRepository.AddAsync(new StockMovement
                    {
                        ProductId = product.Id,
                        Product = product,
                        MovementType = StockMovementType.Purchase,
                        QuantityChange = item.Quantity,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = balanceAfter,
                        UnitCost = item.UnitCost,
                        SourceType = "Purchase",
                        SourceId = purchase.Id.ToString(),
                        Reference = purchase.PurchaseNumber,
                        OccurredAtUtc = postedAtUtc,
                        CreatedBy = _currentUserService.Username
                    }, transactionToken);
                }

                purchase.Status = PurchaseStatus.Posted;
                purchase.AmountPaid = 0;
                purchase.BalanceDue = purchase.GrandTotal;
                purchase.PostedAtUtc = postedAtUtc;
                await _purchaseRepository.SaveChangesAsync(transactionToken);
                postedPurchase = purchase;
            }, cancellationToken);

            return postedPurchase!.ToResponse();
        }

        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
