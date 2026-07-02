using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductAsync(int page, int pagesize, string? search, CancellationToken cancellationToken = default)
        {
            var query = BuildSearchQuery(search);
            
            return await query
                        .Skip((page - 1) * pagesize)
                        .Take(pagesize)
                        .ToListAsync(cancellationToken);
        }

        public async Task<int> GetProductCountAsync(string? search, CancellationToken cancellationToken = default)
        {
            return await BuildSearchQuery(search).CountAsync(cancellationToken);
        }

        public Task<List<Product>> GetCurrentStockReportAsync(
            int pageNumber,
            int pageSize,
            int? categoryId,
            string? search,
            StockQuantityFilter? stock,
            CancellationToken cancellationToken = default)
        {
            return BuildCurrentStockReportQuery(categoryId, search, stock)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetCurrentStockReportCountAsync(
            int? categoryId,
            string? search,
            StockQuantityFilter? stock,
            CancellationToken cancellationToken = default)
        {
            return BuildCurrentStockReportQuery(categoryId, search, stock)
                .CountAsync(cancellationToken);
        }

        private IQueryable<Product> BuildSearchQuery(string? search)
        {
            IQueryable<Product> query = _context.Products
                .Include(x => x.Category);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            return query;
        }

        private IQueryable<Product> BuildCurrentStockReportQuery(
            int? categoryId,
            string? search,
            StockQuantityFilter? stock)
        {
            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Include(x => x.Category);

            if (categoryId.HasValue)
                query = query.Where(x => x.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Name.Contains(search) || x.SKU.Contains(search));

            if (stock == StockQuantityFilter.Positive)
                query = query.Where(x => x.Quantity > 0);
            else if (stock == StockQuantityFilter.Zero)
                query = query.Where(x => x.Quantity == 0);

            return query;
        }

        public async Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task AddProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            await _context.Products.AddAsync(product, cancellationToken);
        }

        public async Task UpdateProductAsync(int id, Product product, CancellationToken cancellationToken = default)
        {
            await SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteProductAsync(Product product, CancellationToken cancellationToken = default)
        {
            _context.Products.Remove(product);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
