using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IUnitRepository
    {
        Task<List<Unit>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Unit?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Unit?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task AddAsync(Unit unit, CancellationToken cancellationToken = default);
        Task DeleteAsync(Unit unit, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
