using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Features.StockMovements
{
    internal static class ManualCorrectionMapping
    {
        public static ManualCorrectionResponse ToManualCorrectionResponse(
            this StockMovement movement)
        {
            return new ManualCorrectionResponse
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                ProductName = movement.Product.Name,
                MovementType = movement.MovementType,
                QuantityChange = movement.QuantityChange,
                BalanceBefore = movement.BalanceBefore,
                BalanceAfter = movement.BalanceAfter,
                UnitCost = movement.UnitCost,
                SourceType = movement.SourceType,
                SourceId = movement.SourceId,
                Reference = movement.Reference,
                Reason = movement.Reason,
                Note = movement.Note,
                OccurredAtUtc = movement.OccurredAtUtc,
                CreatedBy = movement.CreatedBy
            };
        }
    }
}
