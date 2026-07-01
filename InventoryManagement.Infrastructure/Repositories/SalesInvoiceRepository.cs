using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class SalesInvoiceRepository : ISalesInvoiceRepository
    {
        private readonly ApplicationDbContext _context;

        public SalesInvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<SalesInvoice?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.SalesInvoices
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> InvoiceNumberExistsAsync(
            string invoiceNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.SalesInvoices.AnyAsync(
                x => x.InvoiceNumber == invoiceNumber,
                cancellationToken);
        }

        public Task<DeliveryChallanItem?> GetDeliveryChallanItemAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.DeliveryChallanItems
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task AddAsync(
            SalesInvoice invoice,
            CancellationToken cancellationToken = default)
        {
            await _context.SalesInvoices.AddAsync(invoice, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
