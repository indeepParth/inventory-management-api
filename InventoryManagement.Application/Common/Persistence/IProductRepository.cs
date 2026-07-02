using InventoryManagement.Domain.Entities;
using InventoryManagement.Application.Features.InventoryReports.GetCurrentStock;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllProductAsync(int page, int pagesize, string? search, CancellationToken cancellationToken = default);
        Task<int> GetProductCountAsync(string? search, CancellationToken cancellationToken = default);
        Task<List<Product>> GetCurrentStockReportAsync(
            int pageNumber,
            int pageSize,
            int? categoryId,
            string? search,
            StockQuantityFilter? stock,
            CancellationToken cancellationToken = default);
        Task<int> GetCurrentStockReportCountAsync(
            int? categoryId,
            string? search,
            StockQuantityFilter? stock,
            CancellationToken cancellationToken = default);
        Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddProductAsync(Product product, CancellationToken cancellationToken = default);
        Task UpdateProductAsync(int id, Product product, CancellationToken cancellationToken = default);
        Task DeleteProductAsync(Product product, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
