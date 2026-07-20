using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
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

        public async Task<List<SalesInvoice>> GetAllAsync(
            int pageNumber,
            int pageSize,
            int? customerId,
            SalesInvoiceStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? invoiceNumber,
            CancellationToken cancellationToken = default)
        {
            return await BuildFilteredQuery(
                    customerId,
                    status,
                    dateFrom,
                    dateTo,
                    invoiceNumber)
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetCountAsync(
            int? customerId,
            SalesInvoiceStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? invoiceNumber,
            CancellationToken cancellationToken = default)
        {
            return BuildFilteredQuery(
                    customerId,
                    status,
                    dateFrom,
                    dateTo,
                    invoiceNumber)
                .CountAsync(cancellationToken);
        }

        public Task<List<SalesInvoice>> GetRegisterAsync(
            int pageNumber, int pageSize, int? customerId, int? productId,
            bool? isFromDeliveryChallan, SalesInvoiceStatus? status,
            DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            return BuildRegisterQuery(
                    customerId, productId, isFromDeliveryChallan, status,
                    dateFrom, dateTo)
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetRegisterCountAsync(
            int? customerId, int? productId, bool? isFromDeliveryChallan,
            SalesInvoiceStatus? status, DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            return BuildRegisterQuery(
                    customerId, productId, isFromDeliveryChallan, status,
                    dateFrom, dateTo)
                .CountAsync(cancellationToken);
        }

        public Task<List<SalesInvoice>> GetRegisterSummaryAsync(
            int? customerId, int? productId, bool? isFromDeliveryChallan,
            SalesInvoiceStatus? status, DateTime? dateFrom, DateTime? dateTo,
            CancellationToken cancellationToken = default)
        {
            return BuildRegisterQuery(
                    customerId, productId, isFromDeliveryChallan, status,
                    dateFrom, dateTo)
                .AsNoTracking()
                .Include(x => x.Items)
                .ToListAsync(cancellationToken);
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
                .Include(x => x.Items)
                    .ThenInclude(x => x.DeliveryChallanItem!)
                        .ThenInclude(x => x.DeliveryChallan)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<SalesInvoice?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.SalesInvoices
                .Include(x => x.Customer)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .Include(x => x.Items)
                    .ThenInclude(x => x.DeliveryChallanItem)
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

        public Task<bool> InvoiceNumberExistsForOtherAsync(
            string invoiceNumber,
            int invoiceId,
            CancellationToken cancellationToken = default)
        {
            return _context.SalesInvoices.AnyAsync(
                x => x.Id != invoiceId &&
                     x.InvoiceNumber == invoiceNumber,
                cancellationToken);
        }

        public Task<DeliveryChallanItem?> GetDeliveryChallanItemAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.DeliveryChallanItems
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<List<DeliveryChallanItem>> GetChallanItemsForInvoiceAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken = default)
        {
            return _context.DeliveryChallanItems
                .Include(x => x.Product)
                .Include(x => x.DeliveryChallan)
                    .ThenInclude(x => x.Customer)
                .Include(x => x.SalesInvoiceItems)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);
        }

        public Task<List<DeliveryChallan>> GetLinkedChallansForUpdateAsync(
            int invoiceId,
            CancellationToken cancellationToken = default)
        {
            return _context.DeliveryChallans
                .Include(x => x.Items)
                    .ThenInclude(x => x.SalesInvoiceItems)
                        .ThenInclude(x => x.SalesInvoice)
                .Where(x => x.Items.Any(item =>
                    item.SalesInvoiceItems.Any(invoiceItem =>
                        invoiceItem.SalesInvoiceId == invoiceId)))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            SalesInvoice invoice,
            CancellationToken cancellationToken = default)
        {
            await _context.SalesInvoices.AddAsync(invoice, cancellationToken);
        }

        public void RemoveItems(IEnumerable<SalesInvoiceItem> items)
        {
            _context.SalesInvoiceItems.RemoveRange(items);
        }

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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<SalesInvoice> BuildFilteredQuery(
            int? customerId,
            SalesInvoiceStatus? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? invoiceNumber)
        {
            IQueryable<SalesInvoice> query = _context.SalesInvoices;

            if (customerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(x => x.InvoiceDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(x => x.InvoiceDate <= dateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(invoiceNumber))
            {
                var value = invoiceNumber.Trim();
                query = query.Where(x => x.InvoiceNumber.Contains(value));
            }

            return query;
        }

        private IQueryable<SalesInvoice> BuildRegisterQuery(
            int? customerId, int? productId, bool? isFromDeliveryChallan,
            SalesInvoiceStatus? status, DateTime? dateFrom, DateTime? dateTo)
        {
            var query = BuildFilteredQuery(
                customerId, status, dateFrom, dateTo, null);

            if (productId.HasValue)
            {
                query = query.Where(x =>
                    x.Items.Any(item => item.ProductId == productId.Value));
            }

            if (isFromDeliveryChallan.HasValue)
            {
                query = isFromDeliveryChallan.Value
                    ? query.Where(x =>
                        x.Items.Any(item => item.DeliveryChallanItemId.HasValue))
                    : query.Where(x =>
                        x.Items.All(item => !item.DeliveryChallanItemId.HasValue));
            }

            return query;
        }
    }
}
