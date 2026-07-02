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

        Task AddAsync(
            StockMovement stockMovement,
            CancellationToken cancellationToken = default);

        Task<List<StockMovement>> GetPurchaseMovementsForUpdateAsync(
            int purchaseId,
            CancellationToken cancellationToken = default);

        Task<List<StockMovement>> GetDeliveryChallanMovementsForUpdateAsync(
            int deliveryChallanId,
            CancellationToken cancellationToken = default);

        Task<decimal?> GetDeliveryChallanItemCostAsync(
            int deliveryChallanId,
            int productId,
            CancellationToken cancellationToken = default);

        Task<List<StockMovement>> GetSalesInvoiceMovementsForUpdateAsync(
            int salesInvoiceId,
            CancellationToken cancellationToken = default);

        Task<Product?> GetProductForUpdateAsync(
            int productId,
            CancellationToken cancellationToken = default);

        Task<StockMovement?> GetManualCorrectionForUpdateAsync(
            int movementId,
            CancellationToken cancellationToken = default);

        Task<StockMovement?> GetCorrectionReversalAsync(
            int movementId,
            CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
