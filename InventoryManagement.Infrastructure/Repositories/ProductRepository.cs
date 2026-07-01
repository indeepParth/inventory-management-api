using InventoryManagement.Application.Common.Persistence;
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
