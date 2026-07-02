using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.StockMovements.RecordDamage
{
    public class Handler : IRequestHandler<Command, ManualCorrectionResponse>
    {
        private readonly IStockMovementRepository _movements;
        private readonly ICurrentUserService _currentUser;

        public Handler(
            IStockMovementRepository movements,
            ICurrentUserService currentUser)
        {
            _movements = movements;
            _currentUser = currentUser;
        }

        public async Task<ManualCorrectionResponse> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            StockMovement? created = null;
            await _movements.ExecuteInTransactionAsync(async transactionToken =>
            {
                var product = await _movements.GetProductForUpdateAsync(
                    request.ProductId,
                    transactionToken) ??
                    throw new NotFoundException("Product not found.");
                if (product.Quantity < request.Quantity)
                    throw new BadRequestException(
                        "Damage quantity would result in stock below zero.");

                var before = product.Quantity;
                product.Quantity -= request.Quantity;
                created = new StockMovement
                {
                    ProductId = product.Id,
                    Product = product,
                    MovementType = StockMovementType.Damage,
                    QuantityChange = -request.Quantity,
                    BalanceBefore = before,
                    BalanceAfter = product.Quantity,
                    UnitCost = product.AverageCost,
                    SourceType = "ManualCorrection",
                    Reference = Normalize(request.Reference),
                    Reason = request.Reason.Trim(),
                    Note = Normalize(request.Note),
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedBy = _currentUser.Username
                };
                await _movements.AddAsync(created, transactionToken);
                await _movements.SaveChangesAsync(transactionToken);
            }, cancellationToken);

            return created!.ToManualCorrectionResponse();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
