using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveCompanyService _activeCompany;

        public SupplierRepository(
            ApplicationDbContext context,
            IActiveCompanyService activeCompany)
        {
            _context = context;
            _activeCompany = activeCompany;
        }

        public async Task<List<Supplier>> GetAllAsync(
            int pageNumber,
            int pageSize,
            string? search,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            return await BuildQuery(search, isActive)
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

        public Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Suppliers.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public Task<Supplier?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return _context.Suppliers.FirstOrDefaultAsync(
                x => x.Name == name && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public Task<Supplier?> GetByGstNumberAsync(string gstNumber, CancellationToken cancellationToken = default)
        {
            return _context.Suppliers.FirstOrDefaultAsync(
                x => x.GstNumber == gstNumber && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            supplier.CompanyId = _activeCompany.CompanyId;
            await _context.Suppliers.AddAsync(supplier, cancellationToken);
        }

        public void Delete(Supplier supplier)
        {
            _context.Suppliers.Remove(supplier);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<Supplier> BuildQuery(string? search, bool? isActive)
        {
            IQueryable<Supplier> query = _context.Suppliers
                .Where(x => x.CompanyId == _activeCompany.CompanyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    x.Name.Contains(term) ||
                    (x.ContactPerson != null && x.ContactPerson.Contains(term)) ||
                    (x.Email != null && x.Email.Contains(term)) ||
                    (x.GstNumber != null && x.GstNumber.Contains(term)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            return query;
        }
    }
}
