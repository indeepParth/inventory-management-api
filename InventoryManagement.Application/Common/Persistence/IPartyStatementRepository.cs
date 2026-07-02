using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IPartyStatementRepository
    {
        Task<bool> CustomerExistsAsync(
            int id, CancellationToken cancellationToken = default);
        Task<bool> SupplierExistsAsync(
            int id, CancellationToken cancellationToken = default);
        Task<List<SalesInvoice>> GetCustomerInvoicesThroughAsync(
            int customerId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default);
        Task<List<Purchase>> GetSupplierPurchasesThroughAsync(
            int supplierId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default);
        Task<List<Payment>> GetCustomerPaymentsThroughAsync(
            int customerId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default);
        Task<List<Payment>> GetSupplierPaymentsThroughAsync(
            int supplierId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default);
    }
}
