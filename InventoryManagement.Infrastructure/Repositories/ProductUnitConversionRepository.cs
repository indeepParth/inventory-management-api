using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class ProductUnitConversionRepository : IProductUnitConversionRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductUnitConversionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductUnitConversion>> GetByProductIdAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ProductUnitConversions
                .Include(x => x.Unit)
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.UnitId == x.Product.BaseUnitId)
                .ThenBy(x => x.Unit.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProductUnitConversion?> GetByIdAsync(
            int productId,
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.ProductUnitConversions
                .Include(x => x.Unit)
                .FirstOrDefaultAsync(
                    x => x.ProductId == productId && x.Id == id,
                    cancellationToken);
        }

        public async Task<ProductUnitConversion?> GetByProductAndUnitAsync(
            int productId,
            int unitId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ProductUnitConversions
                .Include(x => x.Unit)
                .FirstOrDefaultAsync(
                    x => x.ProductId == productId && x.UnitId == unitId,
                    cancellationToken);
        }

        public async Task AddAsync(
            ProductUnitConversion conversion,
            CancellationToken cancellationToken = default)
        {
            await _context.ProductUnitConversions.AddAsync(
                conversion,
                cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
