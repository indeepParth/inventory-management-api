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
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .OrderByDescending(x => x.ChallanDate).ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

        public Task<int> GetCountAsync(
            int? customerId, DeliveryChallanStatus? status,
            DateTime? dateFrom, DateTime? dateTo, string? challanNumber,
            CancellationToken cancellationToken = default) =>
            Query(customerId, status, dateFrom, dateTo, challanNumber)
                .CountAsync(cancellationToken);

        public Task<DeliveryChallan?> GetByIdAsync(
            int id, CancellationToken cancellationToken = default) =>
            _context.DeliveryChallans.AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<bool> ChallanNumberExistsAsync(
            string challanNumber, CancellationToken cancellationToken = default) =>
            _context.DeliveryChallans.AnyAsync(
                x => x.ChallanNumber == challanNumber, cancellationToken);

        public async Task AddAsync(
            DeliveryChallan challan, CancellationToken cancellationToken = default) =>
            await _context.DeliveryChallans.AddAsync(challan, cancellationToken);

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
    }
}
