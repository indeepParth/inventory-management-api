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
