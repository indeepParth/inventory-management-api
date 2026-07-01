using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface ISupplierRepository
    {
        Task<List<Supplier>> GetAllAsync(
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default);
        Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Supplier?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<Supplier?> GetByGstNumberAsync(string gstNumber, CancellationToken cancellationToken = default);
        Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
        void Delete(Supplier supplier);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
