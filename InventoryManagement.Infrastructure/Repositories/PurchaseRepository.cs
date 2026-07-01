using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
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

        public Task<bool> PurchaseNumberExistsAsync(
            string purchaseNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.Purchases.AnyAsync(
                x => x.PurchaseNumber == purchaseNumber,
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

        public async Task AddAsync(
            Purchase purchase,
            CancellationToken cancellationToken = default)
        {
            await _context.Purchases.AddAsync(purchase, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
