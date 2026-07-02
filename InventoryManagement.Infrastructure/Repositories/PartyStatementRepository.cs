using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class PartyStatementRepository : IPartyStatementRepository
    {
        private readonly ApplicationDbContext _context;
        public PartyStatementRepository(ApplicationDbContext context) => _context = context;

        public Task<bool> CustomerExistsAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.Customers.AnyAsync(x => x.Id == id, cancellationToken);

        public Task<bool> SupplierExistsAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.Suppliers.AnyAsync(x => x.Id == id, cancellationToken);

        public Task<List<SalesInvoice>> GetCustomerInvoicesThroughAsync(
            int customerId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default) =>
            _context.SalesInvoices.AsNoTracking()
                .Where(x => x.CustomerId == customerId &&
                    x.InvoiceDate < dateToExclusive &&
                    (x.Status == SalesInvoiceStatus.Posted ||
                     x.Status == SalesInvoiceStatus.PartiallyPaid ||
                     x.Status == SalesInvoiceStatus.Paid))
                .ToListAsync(cancellationToken);

        public Task<List<Purchase>> GetSupplierPurchasesThroughAsync(
            int supplierId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default) =>
            _context.Purchases.AsNoTracking()
                .Where(x => x.SupplierId == supplierId &&
                    x.BillDate < dateToExclusive &&
                    (x.Status == PurchaseStatus.Posted ||
                     x.Status == PurchaseStatus.PartiallyPaid ||
                     x.Status == PurchaseStatus.Paid))
                .ToListAsync(cancellationToken);

        public Task<List<Payment>> GetCustomerPaymentsThroughAsync(
            int customerId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default) =>
            _context.Payments.AsNoTracking()
                .Where(x => x.CustomerId == customerId &&
                            x.PaymentDate < dateToExclusive)
                .ToListAsync(cancellationToken);

        public Task<List<Payment>> GetSupplierPaymentsThroughAsync(
            int supplierId, DateTime dateToExclusive,
            CancellationToken cancellationToken = default) =>
            _context.Payments.AsNoTracking()
                .Where(x => x.SupplierId == supplierId &&
                            x.PaymentDate < dateToExclusive)
                .ToListAsync(cancellationToken);
    }
}
