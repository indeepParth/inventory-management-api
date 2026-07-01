using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.CancelDeliveryChallan
{
    public class Handler : IRequestHandler<Command, DeliveryChallanResponse>
    {
        private readonly IDeliveryChallanRepository _challans;
        private readonly IStockMovementRepository _movements;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            IDeliveryChallanRepository challans,
            IStockMovementRepository movements,
            ICurrentUserService currentUser)
        {
            _challans = challans;
            _movements = movements;
            _currentUser = currentUser;
        }

        public async Task<DeliveryChallanResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            DeliveryChallan? cancelled = null;

            await _challans.ExecuteInTransactionAsync(async transactionToken =>
            {
                var challan = await _challans.GetForUpdateAsync(
                    request.Id, transactionToken) ??
                    throw new NotFoundException("Delivery challan not found.");

                if (challan.Status == DeliveryChallanStatus.Cancelled)
                {
                    cancelled = challan;
                    return;
                }

                if (challan.Status == DeliveryChallanStatus.Invoiced)
                    throw new BadRequestException(
                        "Invoiced delivery challans cannot be cancelled directly.");

                if (challan.Status == DeliveryChallanStatus.Posted)
                    await ReversePostedChallanAsync(challan, transactionToken);
                else if (challan.Status != DeliveryChallanStatus.Draft)
                    throw new BadRequestException(
                        "Delivery challan cannot be cancelled.");

                var cancelledAtUtc = DateTime.UtcNow;
                challan.Status = DeliveryChallanStatus.Cancelled;
                challan.CancelledAtUtc = cancelledAtUtc;
                challan.UpdatedAtUtc = cancelledAtUtc;
                await _challans.SaveChangesAsync(transactionToken);
                cancelled = challan;
            }, cancellationToken);

            return cancelled!.ToResponse();
        }

        private async Task ReversePostedChallanAsync(
            DeliveryChallan challan,
            CancellationToken cancellationToken)
        {
            var originalMovements =
                await _movements.GetDeliveryChallanMovementsForUpdateAsync(
                    challan.Id, cancellationToken);

            if (originalMovements.Count == 0)
                throw new BadRequestException(
                    "Posted delivery challan movements could not be found.");

            var occurredAtUtc = DateTime.UtcNow;
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
                    SourceType = "DeliveryChallanCancellation",
                    SourceId = challan.Id.ToString(),
                    Reference = challan.ChallanNumber,
                    OccurredAtUtc = occurredAtUtc,
                    CreatedBy = _currentUser.Username
                }, cancellationToken);
            }
        }
    }
}
