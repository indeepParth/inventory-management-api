using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface ISalesInvoiceRepository
    {
        Task<List<SalesInvoice>> GetAllAsync(
            int pageNumber,
            int pageSize,
            int? customerId,
            SalesInvoiceStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? invoiceNumber,
            CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(
            int? customerId,
            SalesInvoiceStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? invoiceNumber,
            CancellationToken cancellationToken = default);
        Task<SalesInvoice?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<SalesInvoice?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<bool> InvoiceNumberExistsAsync(
            string invoiceNumber,
            CancellationToken cancellationToken = default);
        Task<bool> InvoiceNumberExistsForOtherAsync(
            string invoiceNumber,
            int invoiceId,
            CancellationToken cancellationToken = default);
        Task<DeliveryChallanItem?> GetDeliveryChallanItemAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task AddAsync(
            SalesInvoice invoice,
            CancellationToken cancellationToken = default);
        void RemoveItems(IEnumerable<SalesInvoiceItem> items);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
