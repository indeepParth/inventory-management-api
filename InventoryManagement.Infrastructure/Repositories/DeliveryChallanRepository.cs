using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class DeliveryChallanRepository : IDeliveryChallanRepository
    {
        private readonly ApplicationDbContext _context;

        public DeliveryChallanRepository(ApplicationDbContext context) => _context = context;

        public Task<List<DeliveryChallan>> GetAllAsync(
            int pageNumber, int pageSize, int? customerId,
            DeliveryChallanStatus? status, DateTime? dateFrom, DateTime? dateTo,
            string? challanNumber, CancellationToken cancellationToken = default) =>
            Query(customerId, status, dateFrom, dateTo, challanNumber)
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Driver)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .Include(x => x.Items)
                    .ThenInclude(x => x.SalesInvoiceItems)
                .OrderByDescending(x => x.ChallanDate).ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

        public Task<int> GetCountAsync(
            int? customerId, DeliveryChallanStatus? status,
            DateTime? dateFrom, DateTime? dateTo, string? challanNumber,
            CancellationToken cancellationToken = default) =>
            Query(customerId, status, dateFrom, dateTo, challanNumber)
                .CountAsync(cancellationToken);

        public Task<List<DeliveryChallan>> GetDriverDeliveriesAsync(
            int driverId,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool? isDeliveryChargePaid,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            DriverDeliveriesQuery(driverId, dateFrom, dateTo, isDeliveryChargePaid)
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .OrderByDescending(x => x.ChallanDate)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        public Task<int> GetDriverDeliveriesCountAsync(
            int driverId,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool? isDeliveryChargePaid,
            CancellationToken cancellationToken = default) =>
            DriverDeliveriesQuery(driverId, dateFrom, dateTo, isDeliveryChargePaid)
                .CountAsync(cancellationToken);

        public Task<DeliveryChallan?> GetByIdAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.DeliveryChallans.AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Driver)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .Include(x => x.Items)
                    .ThenInclude(x => x.SalesInvoiceItems)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<DeliveryChallan?> GetForUpdateAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.DeliveryChallans
                .Include(x => x.Customer)
                .Include(x => x.Driver)
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<bool> ChallanNumberExistsAsync(
            string challanNumber, CancellationToken cancellationToken = default) =>
            _context.DeliveryChallans.AnyAsync(
                x => x.ChallanNumber == challanNumber, cancellationToken);

        public Task<bool> ChallanNumberExistsForOtherAsync(
            string challanNumber, int deliveryChallanId,
            CancellationToken cancellationToken = default) =>
            _context.DeliveryChallans.AnyAsync(
                x => x.Id != deliveryChallanId &&
                     x.ChallanNumber == challanNumber,
                cancellationToken);

        public async Task AddAsync(
            DeliveryChallan challan, CancellationToken cancellationToken = default) =>
            await _context.DeliveryChallans.AddAsync(challan, cancellationToken);

        public void RemoveItems(IEnumerable<DeliveryChallanItem> items) =>
            _context.DeliveryChallanItems.RemoveRange(items);

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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        private IQueryable<DeliveryChallan> Query(
            int? customerId, DeliveryChallanStatus? status,
            DateTime? dateFrom, DateTime? dateTo, string? challanNumber)
        {
            IQueryable<DeliveryChallan> query = _context.DeliveryChallans;
            if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId);
            if (status.HasValue) query = query.Where(x => x.Status == status);
            if (dateFrom.HasValue) query = query.Where(x => x.ChallanDate >= dateFrom);
            if (dateTo.HasValue) query = query.Where(x => x.ChallanDate <= dateTo);
            if (!string.IsNullOrWhiteSpace(challanNumber))
            {
                var value = challanNumber.Trim();
                query = query.Where(x => x.ChallanNumber.Contains(value));
            }
            return query;
        }

        private IQueryable<DeliveryChallan> DriverDeliveriesQuery(
            int driverId,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool? isDeliveryChargePaid)
        {
            IQueryable<DeliveryChallan> query = _context.DeliveryChallans
                .Where(x => x.DriverId == driverId)
                .Where(x =>
                    x.Status == DeliveryChallanStatus.Posted ||
                    x.Status == DeliveryChallanStatus.Invoiced);

            if (dateFrom.HasValue) query = query.Where(x => x.ChallanDate >= dateFrom);
            if (dateTo.HasValue) query = query.Where(x => x.ChallanDate <= dateTo);
            if (isDeliveryChargePaid.HasValue)
            {
                query = query.Where(
                    x => x.IsDeliveryChargePaid == isDeliveryChargePaid.Value);
            }

            return query;
        }
    }
}
