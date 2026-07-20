using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface ICustomerReturnRepository
    {
        Task<CustomerReturn?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<CustomerReturn?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<List<CustomerReturn>> GetBySalesInvoiceIdAsync(
            int salesInvoiceId,
            CancellationToken cancellationToken = default);
        Task<SalesInvoice?> GetInvoiceForReturnAsync(
            int invoiceId,
            CancellationToken cancellationToken = default);
        Task<bool> ReturnNumberExistsAsync(
            string returnNumber,
            CancellationToken cancellationToken = default);
        Task<Dictionary<int, decimal>> GetPostedReturnedQuantitiesAsync(
            IReadOnlyCollection<int> invoiceItemIds,
            int? excludingReturnId,
            CancellationToken cancellationToken = default);
        Task AddAsync(
            CustomerReturn customerReturn,
            CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
