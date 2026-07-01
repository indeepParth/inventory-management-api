using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IPurchaseRepository
    {
        Task<List<Purchase>> GetAllAsync(
            int pageNumber,
            int pageSize,
            int? supplierId,
            PurchaseStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? purchaseNumber,
            string? supplierBillNumber,
            CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(
            int? supplierId,
            PurchaseStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? purchaseNumber,
            string? supplierBillNumber,
            CancellationToken cancellationToken = default);
        Task<Purchase?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Purchase?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> PurchaseNumberExistsAsync(
            string purchaseNumber,
            CancellationToken cancellationToken = default);
        Task<bool> PurchaseNumberExistsForOtherAsync(
            string purchaseNumber,
            int purchaseId,
            CancellationToken cancellationToken = default);
        Task<bool> SupplierBillNumberExistsAsync(
            int supplierId,
            string supplierBillNumber,
            CancellationToken cancellationToken = default);
        Task<bool> SupplierBillNumberExistsForOtherAsync(
            int supplierId,
            string supplierBillNumber,
            int purchaseId,
            CancellationToken cancellationToken = default);
        Task AddAsync(Purchase purchase, CancellationToken cancellationToken = default);
        void RemoveItems(IEnumerable<PurchaseItem> items);
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
