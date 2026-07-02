using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;
        public PaymentRepository(ApplicationDbContext context) => _context = context;

        public Task<List<Payment>> GetAllAsync(
            int pageNumber, int pageSize, int? customerId, int? salesInvoiceId,
            int? supplierId, int? purchaseId,
            PaymentMethod? method, DateTime? dateFrom, DateTime? dateTo,
            string? receiptNumber, CancellationToken cancellationToken = default) =>
            Filter(customerId, salesInvoiceId, supplierId, purchaseId, method,
                    dateFrom, dateTo, receiptNumber)
                .AsNoTracking().Include(x => x.Customer).Include(x => x.SalesInvoice)
                .Include(x => x.Supplier).Include(x => x.Purchase)
                .Include(x => x.Reversal).OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToListAsync(cancellationToken);

        public Task<int> GetCountAsync(
            int? customerId, int? salesInvoiceId, int? supplierId, int? purchaseId,
            PaymentMethod? method,
            DateTime? dateFrom, DateTime? dateTo, string? receiptNumber,
            CancellationToken cancellationToken = default) =>
            Filter(customerId, salesInvoiceId, supplierId, purchaseId, method,
                    dateFrom, dateTo, receiptNumber)
                .CountAsync(cancellationToken);

        public Task<bool> ReceiptNumberExistsAsync(
            string receiptNumber, CancellationToken cancellationToken = default) =>
            _context.Payments.AnyAsync(x => x.ReceiptNumber == receiptNumber, cancellationToken);

        public Task<Customer?> GetCustomerForUpdateAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<SalesInvoice?> GetInvoiceForUpdateAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.SalesInvoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<Supplier?> GetSupplierForUpdateAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<Purchase?> GetPurchaseForUpdateAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.Purchases.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<Payment?> GetForReversalAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.Payments.Include(x => x.Customer).Include(x => x.SalesInvoice)
                .Include(x => x.Supplier).Include(x => x.Purchase)
                .Include(x => x.Reversal).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
            _context.Payments.AddAsync(payment, cancellationToken).AsTask();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        private IQueryable<Payment> Filter(
            int? customerId, int? salesInvoiceId, int? supplierId, int? purchaseId,
            PaymentMethod? method,
            DateTime? dateFrom, DateTime? dateTo, string? receiptNumber)
        {
            IQueryable<Payment> query = _context.Payments;
            if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId);
            if (salesInvoiceId.HasValue) query = query.Where(x => x.SalesInvoiceId == salesInvoiceId);
            if (supplierId.HasValue) query = query.Where(x => x.SupplierId == supplierId);
            if (purchaseId.HasValue) query = query.Where(x => x.PurchaseId == purchaseId);
            if (method.HasValue) query = query.Where(x => x.Method == method);
            if (dateFrom.HasValue) query = query.Where(x => x.PaymentDate >= dateFrom);
            if (dateTo.HasValue) query = query.Where(x => x.PaymentDate <= dateTo);
            if (!string.IsNullOrWhiteSpace(receiptNumber))
            {
                var value = receiptNumber.Trim();
                query = query.Where(x => x.ReceiptNumber.Contains(value));
            }
            return query;
        }
    }
}
