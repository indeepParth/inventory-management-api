using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Categories.FindAsync(id, cancellationToken);
        }

        public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Categories.AnyAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Products.AnyAsync(x => x.CategoryId == id, cancellationToken);
        }

        public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            await _context.Categories.AddAsync(category, cancellationToken);
        }

        public async Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
        {
            _context.Categories.Remove(category);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
