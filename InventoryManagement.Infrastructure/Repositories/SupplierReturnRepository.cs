using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class SupplierReturnRepository : ISupplierReturnRepository
    {
        private readonly ApplicationDbContext _context;

        public SupplierReturnRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<SupplierReturn?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<SupplierReturn?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Query()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Purchase?> GetPurchaseForReturnAsync(
            int purchaseId,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases
                .Include(x => x.Supplier)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == purchaseId, cancellationToken);
        }

        public Task<bool> ReturnNumberExistsAsync(
            string returnNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.SupplierReturns.AnyAsync(
                x => x.ReturnNumber == returnNumber,
                cancellationToken);
        }

        public async Task<Dictionary<int, decimal>> GetPostedReturnedQuantitiesAsync(
            IReadOnlyCollection<int> purchaseItemIds,
            int? excludingReturnId,
            CancellationToken cancellationToken = default)
        {
            var quantities = await _context.SupplierReturnItems
                .Where(x =>
                    purchaseItemIds.Contains(x.PurchaseItemId) &&
                    x.SupplierReturn.Status == SupplierReturnStatus.Posted &&
                    (!excludingReturnId.HasValue ||
                     x.SupplierReturnId != excludingReturnId.Value))
                .Select(x => new { x.PurchaseItemId, x.Quantity })
                .ToListAsync(cancellationToken);

            return quantities
                .GroupBy(x => x.PurchaseItemId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(item => item.Quantity));
        }

        public async Task AddAsync(
            SupplierReturn supplierReturn,
            CancellationToken cancellationToken = default)
        {
            await _context.SupplierReturns.AddAsync(
                supplierReturn,
                cancellationToken);
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<SupplierReturn> Query()
        {
            return _context.SupplierReturns
                .Include(x => x.Supplier)
                .Include(x => x.Purchase)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .Include(x => x.Items)
                    .ThenInclude(x => x.PurchaseItem);
        }
    }
}
