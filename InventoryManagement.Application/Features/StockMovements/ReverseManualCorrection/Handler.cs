using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using MediatR;

namespace InventoryManagement.Application.Features.StockMovements.ReverseManualCorrection
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
            StockMovement? reversal = null;
            await _movements.ExecuteInTransactionAsync(async transactionToken =>
            {
                reversal = await _movements.GetCorrectionReversalAsync(
                    request.Id,
                    transactionToken);
                if (reversal is not null)
                    return;

                var original = await _movements.GetManualCorrectionForUpdateAsync(
                    request.Id,
                    transactionToken) ??
                    throw new NotFoundException(
                        "Manual inventory correction not found.");
                var quantityChange = -original.QuantityChange;
                var product = original.Product;
                var after = product.Quantity + quantityChange;
                if (after < 0)
                    throw new BadRequestException(
                        "Reversal would result in stock below zero.");

                var before = product.Quantity;
                product.Quantity = after;
                reversal = new StockMovement
                {
                    ProductId = product.Id,
                    Product = product,
                    MovementType = StockMovementType.Reversal,
                    QuantityChange = quantityChange,
                    BalanceBefore = before,
                    BalanceAfter = after,
                    UnitCost = original.UnitCost,
                    SourceType = "ManualCorrectionReversal",
                    SourceId = original.Id.ToString(),
                    Reference = Normalize(request.Reference),
                    Reason = request.Reason.Trim(),
                    Note = Normalize(request.Note),
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedBy = _currentUser.Username
                };
                await _movements.AddAsync(reversal, transactionToken);
                await _movements.SaveChangesAsync(transactionToken);
            }, cancellationToken);

            return reversal!.ToManualCorrectionResponse();
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
