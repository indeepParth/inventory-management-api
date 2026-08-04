using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IProductUnitConversionRepository
    {
        Task<List<ProductUnitConversion>> GetByProductIdAsync(
            int productId,
            CancellationToken cancellationToken = default);
        Task<ProductUnitConversion?> GetByIdAsync(
            int productId,
            int id,
            CancellationToken cancellationToken = default);
        Task<ProductUnitConversion?> GetByProductAndUnitAsync(
            int productId,
            int unitId,
            CancellationToken cancellationToken = default);
        Task AddAsync(
            ProductUnitConversion conversion,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
