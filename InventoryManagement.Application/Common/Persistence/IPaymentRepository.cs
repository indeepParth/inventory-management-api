using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetAllAsync(
            int pageNumber, int pageSize, int? customerId, int? salesInvoiceId,
            int? supplierId, int? purchaseId,
            PaymentMethod? method, DateTime? dateFrom, DateTime? dateTo,
            string? receiptNumber, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(
            int? customerId, int? salesInvoiceId, int? supplierId, int? purchaseId,
            PaymentMethod? method,
            DateTime? dateFrom, DateTime? dateTo, string? receiptNumber,
            CancellationToken cancellationToken = default);
        Task<bool> ReceiptNumberExistsAsync(
            string receiptNumber, CancellationToken cancellationToken = default);
        Task<Customer?> GetCustomerForUpdateAsync(
            int id, CancellationToken cancellationToken = default);
        Task<SalesInvoice?> GetInvoiceForUpdateAsync(
            int id, CancellationToken cancellationToken = default);
        Task<Supplier?> GetSupplierForUpdateAsync(
            int id, CancellationToken cancellationToken = default);
        Task<Purchase?> GetPurchaseForUpdateAsync(
            int id, CancellationToken cancellationToken = default);
        Task<Payment?> GetForReversalAsync(
            int id, CancellationToken cancellationToken = default);
        Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
    }
}
