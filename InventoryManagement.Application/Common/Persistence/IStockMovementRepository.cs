using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IStockMovementRepository
    {
        Task<List<StockMovement>> GetAsync(
            int pageNumber,
            int pageSize,
            int? productId,
            StockMovementType? movementType,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            int? productId,
            StockMovementType? movementType,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default);
    }
}
