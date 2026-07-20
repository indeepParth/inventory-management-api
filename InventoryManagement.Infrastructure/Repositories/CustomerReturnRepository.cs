using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class CustomerReturnRepository : ICustomerReturnRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerReturnRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<CustomerReturn?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Query().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<CustomerReturn?> GetForUpdateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Query()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<List<CustomerReturn>> GetBySalesInvoiceIdAsync(
            int salesInvoiceId,
            CancellationToken cancellationToken = default)
        {
            return Query().AsNoTracking()
                .Where(x => x.SalesInvoiceId == salesInvoiceId)
                .OrderByDescending(x => x.ReturnDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<SalesInvoice?> GetInvoiceForReturnAsync(
            int invoiceId,
            CancellationToken cancellationToken = default)
        {
            return _context.SalesInvoices
                .Include(x => x.Customer)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        }

        public Task<bool> ReturnNumberExistsAsync(
            string returnNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.CustomerReturns.AnyAsync(
                x => x.ReturnNumber == returnNumber,
                cancellationToken);
        }

        public async Task<Dictionary<int, decimal>> GetPostedReturnedQuantitiesAsync(
            IReadOnlyCollection<int> invoiceItemIds,
            int? excludingReturnId,
            CancellationToken cancellationToken = default)
        {
            var quantities = await _context.CustomerReturnItems
                .Where(x =>
                    invoiceItemIds.Contains(x.SalesInvoiceItemId) &&
                    x.CustomerReturn.Status == CustomerReturnStatus.Posted &&
                    (!excludingReturnId.HasValue ||
                     x.CustomerReturnId != excludingReturnId.Value))
                .Select(x => new
                {
                    x.SalesInvoiceItemId,
                    x.Quantity
                })
                .ToListAsync(cancellationToken);

            return quantities
                .GroupBy(x => x.SalesInvoiceItemId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(item => item.Quantity));
        }

        public async Task AddAsync(
            CustomerReturn customerReturn,
            CancellationToken cancellationToken = default)
        {
            await _context.CustomerReturns.AddAsync(
                customerReturn,
                cancellationToken);
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

        private IQueryable<CustomerReturn> Query()
        {
            return _context.CustomerReturns
                .Include(x => x.Customer)
                .Include(x => x.SalesInvoice)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .Include(x => x.Items)
                    .ThenInclude(x => x.SalesInvoiceItem);
        }
    }
}
