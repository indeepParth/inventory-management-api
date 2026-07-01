using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.DeliveryChallans.PostDeliveryChallan
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
            Command request, CancellationToken cancellationToken)
        {
            DeliveryChallan? posted = null;
            await _challans.ExecuteInTransactionAsync(async transactionToken =>
            {
                var challan = await _challans.GetForUpdateAsync(
                    request.Id, transactionToken) ??
                    throw new NotFoundException("Delivery challan not found.");
                if (challan.Status != DeliveryChallanStatus.Draft)
                    throw new BadRequestException(
                        "Only Draft delivery challans may be posted.");

                foreach (var group in challan.Items.GroupBy(x => x.ProductId))
                {
                    var required = group.Sum(x => x.Quantity);
                    if (group.First().Product.Quantity < required)
                        throw new BadRequestException(
                            $"Insufficient stock for product {group.Key}.");
                }

                var postedAtUtc = DateTime.UtcNow;
                foreach (var item in challan.Items)
                {
                    var product = item.Product;
                    var before = product.Quantity;
                    product.Quantity -= item.Quantity;
                    await _movements.AddAsync(new StockMovement
                    {
                        ProductId = product.Id,
                        Product = product,
                        MovementType = StockMovementType.Sale,
                        QuantityChange = -item.Quantity,
                        BalanceBefore = before,
                        BalanceAfter = product.Quantity,
                        UnitCost = product.AverageCost,
                        SourceType = "DeliveryChallan",
                        SourceId = challan.Id.ToString(),
                        Reference = challan.ChallanNumber,
                        OccurredAtUtc = postedAtUtc,
                        CreatedBy = _currentUser.Username
                    }, transactionToken);
                }

                challan.Status = DeliveryChallanStatus.Posted;
                challan.PostedAtUtc = postedAtUtc;
                challan.UpdatedAtUtc = postedAtUtc;
                await _challans.SaveChangesAsync(transactionToken);
                posted = challan;
            }, cancellationToken);

            return posted!.ToResponse();
        }
    }
}
