using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveCompanyService _activeCompany;

        public DriverRepository(
            ApplicationDbContext context,
            IActiveCompanyService activeCompany)
        {
            _context = context;
            _activeCompany = activeCompany;
        }

        public Task<List<Driver>> GetAllAsync(
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            return BuildQuery(search, isActive)
                .OrderBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetCountAsync(
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            return BuildQuery(search, isActive).CountAsync(cancellationToken);
        }

        public Task<Driver?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Drivers.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public Task<Driver?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return _context.Drivers.FirstOrDefaultAsync(
                x => x.Name == name && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public async Task AddAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            driver.CompanyId = _activeCompany.CompanyId;
            await _context.Drivers.AddAsync(driver, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<Driver> BuildQuery(string? search, bool? isActive)
        {
            IQueryable<Driver> query = _context.Drivers
                .Where(x => x.CompanyId == _activeCompany.CompanyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    x.Name.Contains(term) ||
                    (x.Phone != null && x.Phone.Contains(term)) ||
                    (x.LicenseNumber != null && x.LicenseNumber.Contains(term)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            return query;
        }
    }
}
