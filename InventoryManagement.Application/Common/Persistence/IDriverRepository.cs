using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IDriverRepository
    {
        Task<List<Driver>> GetAllAsync(
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default);
        Task<Driver?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Driver?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task AddAsync(Driver driver, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
