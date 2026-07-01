using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IPurchaseRepository
    {
        Task<Purchase?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> PurchaseNumberExistsAsync(
            string purchaseNumber,
            CancellationToken cancellationToken = default);
        Task<bool> SupplierBillNumberExistsAsync(
            int supplierId,
            string supplierBillNumber,
            CancellationToken cancellationToken = default);
        Task AddAsync(Purchase purchase, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
