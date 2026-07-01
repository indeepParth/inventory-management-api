using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Persistence
{
    public interface ISalesInvoiceRepository
    {
        Task<SalesInvoice?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task<bool> InvoiceNumberExistsAsync(
            string invoiceNumber,
            CancellationToken cancellationToken = default);
        Task<DeliveryChallanItem?> GetDeliveryChallanItemAsync(
            int id,
            CancellationToken cancellationToken = default);
        Task AddAsync(
            SalesInvoice invoice,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
