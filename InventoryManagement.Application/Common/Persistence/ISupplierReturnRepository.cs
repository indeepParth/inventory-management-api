using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface ISupplierReturnRepository
    {
        Task<SupplierReturn?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<SupplierReturn?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<Purchase?> GetPurchaseForReturnAsync(
            int purchaseId,
            CancellationToken cancellationToken = default);
        Task<bool> ReturnNumberExistsAsync(
            string returnNumber,
            CancellationToken cancellationToken = default);
        Task<Dictionary<int, decimal>> GetPostedReturnedQuantitiesAsync(
            IReadOnlyCollection<int> purchaseItemIds,
            int? excludingReturnId,
            CancellationToken cancellationToken = default);
        Task AddAsync(
            SupplierReturn supplierReturn,
            CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
