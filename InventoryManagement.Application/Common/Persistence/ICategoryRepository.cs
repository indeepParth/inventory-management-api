using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> HasProductsAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(Category category, CancellationToken cancellationToken = default);
        Task DeleteAsync(Category category, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
