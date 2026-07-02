using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly ApplicationDbContext _context;

        public PurchaseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Purchase>> GetAllAsync(
            int pageNumber,
            int pageSize,
            int? supplierId,
            PurchaseStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? purchaseNumber,
            string? supplierBillNumber,
            CancellationToken cancellationToken = default)
        {
            return await BuildFilteredQuery(
                    supplierId,
                    status,
                    dateFrom,
                    dateTo,
                    purchaseNumber,
                    supplierBillNumber)
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .OrderByDescending(x => x.BillDate)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetCountAsync(
            int? supplierId,
            PurchaseStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? purchaseNumber,
            string? supplierBillNumber,
            CancellationToken cancellationToken = default)
        {
            return BuildFilteredQuery(
                    supplierId,
                    status,
                    dateFrom,
                    dateTo,
                    purchaseNumber,
                    supplierBillNumber)
                .CountAsync(cancellationToken);
        }

        public Task<List<Purchase>> GetRegisterAsync(
            int pageNumber, int pageSize, int? supplierId, int? productId,
            PurchaseStatus? status, DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            return BuildRegisterQuery(supplierId, productId, status, dateFrom, dateTo)
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .OrderByDescending(x => x.BillDate).ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetRegisterCountAsync(
            int? supplierId, int? productId, PurchaseStatus? status,
            DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            return BuildRegisterQuery(supplierId, productId, status, dateFrom, dateTo)
                .CountAsync(cancellationToken);
        }

        public Task<List<Purchase>> GetRegisterSummaryAsync(
            int? supplierId, int? productId, PurchaseStatus? status,
            DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            return BuildRegisterQuery(supplierId, productId, status, dateFrom, dateTo)
                .AsNoTracking()
                .Include(x => x.Items)
                .ToListAsync(cancellationToken);
        }

        public Task<Purchase?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Purchase?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases
                .Include(x => x.Supplier)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> PurchaseNumberExistsAsync(
            string purchaseNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases.AnyAsync(
                x => x.PurchaseNumber == purchaseNumber,
                cancellationToken);
        }

        public Task<bool> PurchaseNumberExistsForOtherAsync(
            string purchaseNumber,
            int purchaseId,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases.AnyAsync(
                x => x.Id != purchaseId &&
                     x.PurchaseNumber == purchaseNumber,
                cancellationToken);
        }

        public Task<bool> SupplierBillNumberExistsAsync(
            int supplierId,
            string supplierBillNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases.AnyAsync(
                x => x.SupplierId == supplierId &&
                     x.SupplierBillNumber == supplierBillNumber,
                cancellationToken);
        }

        public Task<bool> SupplierBillNumberExistsForOtherAsync(
            int supplierId,
            string supplierBillNumber,
            int purchaseId,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases.AnyAsync(
                x => x.Id != purchaseId &&
                     x.SupplierId == supplierId &&
                     x.SupplierBillNumber == supplierBillNumber,
                cancellationToken);
        }

        public async Task AddAsync(
            Purchase purchase,
            CancellationToken cancellationToken = default)
        {
            await _context.Purchases.AddAsync(purchase, cancellationToken);
        }

        public void RemoveItems(IEnumerable<PurchaseItem> items)
        {
            _context.PurchaseItems.RemoveRange(items);
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

        private IQueryable<Purchase> BuildFilteredQuery(
            int? supplierId,
            PurchaseStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? purchaseNumber,
            string? supplierBillNumber)
        {
            IQueryable<Purchase> query = _context.Purchases;

            if (supplierId.HasValue)
            {
                query = query.Where(x => x.SupplierId == supplierId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(x => x.BillDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(x => x.BillDate <= dateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(purchaseNumber))
            {
                var value = purchaseNumber.Trim();
                query = query.Where(x => x.PurchaseNumber.Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(supplierBillNumber))
            {
                var value = supplierBillNumber.Trim();
                query = query.Where(x =>
                    x.SupplierBillNumber != null &&
                    x.SupplierBillNumber.Contains(value));
            }

            return query;
        }

        private IQueryable<Purchase> BuildRegisterQuery(
            int? supplierId, int? productId, PurchaseStatus? status,
            DateTime? dateFrom, DateTime? dateTo)
        {
            var query = BuildFilteredQuery(
                supplierId, status, dateFrom, dateTo, null, null);

            if (productId.HasValue)
            {
                query = query.Where(x =>
                    x.Items.Any(item => item.ProductId == productId.Value));
            }

            return query;
        }
    }
}
