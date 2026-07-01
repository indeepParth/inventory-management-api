using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly ApplicationDbContext _context;

        public StockMovementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockMovement>> GetAsync(
            int pageNumber,
            int pageSize,
            int? productId,
            StockMovementType? movementType,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            return await BuildQuery(productId, movementType, fromDate, toDate)
                .OrderByDescending(x => x.OccurredAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountAsync(
            int? productId,
            StockMovementType? movementType,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            return BuildQuery(productId, movementType, fromDate, toDate)
                .CountAsync(cancellationToken);
        }

        public async Task AddAsync(
            StockMovement stockMovement,
            CancellationToken cancellationToken = default)
        {
            await _context.StockMovements.AddAsync(stockMovement, cancellationToken);
        }

        public Task<List<StockMovement>> GetPurchaseMovementsForUpdateAsync(
            int purchaseId,
            CancellationToken cancellationToken = default)
        {
            var sourceId = purchaseId.ToString();

            return _context.StockMovements
                .Include(x => x.Product)
                .Where(x =>
                    x.MovementType == StockMovementType.Purchase &&
                    x.SourceType == "Purchase" &&
                    x.SourceId == sourceId)
                .OrderByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<List<StockMovement>> GetDeliveryChallanMovementsForUpdateAsync(
            int deliveryChallanId,
            CancellationToken cancellationToken = default)
        {
            var sourceId = deliveryChallanId.ToString();

            return _context.StockMovements
                .Include(x => x.Product)
                .Where(x =>
                    x.MovementType == StockMovementType.Sale &&
                    x.SourceType == "DeliveryChallan" &&
                    x.SourceId == sourceId)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<decimal?> GetDeliveryChallanItemCostAsync(
            int deliveryChallanId,
            int productId,
            CancellationToken cancellationToken = default)
        {
            var sourceId = deliveryChallanId.ToString();
            return _context.StockMovements
                .Where(x =>
                    x.MovementType == StockMovementType.Sale &&
                    x.SourceType == "DeliveryChallan" &&
                    x.SourceId == sourceId &&
                    x.ProductId == productId)
                .Select(x => (decimal?)x.UnitCost)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<StockMovement>> GetSalesInvoiceMovementsForUpdateAsync(
            int salesInvoiceId,
            CancellationToken cancellationToken = default)
        {
            var sourceId = salesInvoiceId.ToString();
            return _context.StockMovements
                .Include(x => x.Product)
                .Where(x =>
                    x.MovementType == StockMovementType.Sale &&
                    x.SourceType == "SalesInvoice" &&
                    x.SourceId == sourceId)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }
        private IQueryable<StockMovement> BuildQuery(
            int? productId,
            StockMovementType? movementType,
            DateTime? fromDate,
            DateTime? toDate)
        {
            IQueryable<StockMovement> query = _context.StockMovements
                .AsNoTracking()
                .Include(x => x.Product);

            if (productId.HasValue)
                query = query.Where(x => x.ProductId == productId.Value);

            if (movementType.HasValue)
                query = query.Where(x => x.MovementType == movementType.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.OccurredAtUtc >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.OccurredAtUtc <= toDate.Value);

            return query;
        }
    }
}
