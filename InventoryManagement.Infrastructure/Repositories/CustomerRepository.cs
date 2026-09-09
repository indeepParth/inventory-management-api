using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveCompanyService _activeCompany;

        public CustomerRepository(
            ApplicationDbContext context,
            IActiveCompanyService activeCompany)
        {
            _context = context;
            _activeCompany = activeCompany;
        }

        public Task<List<Customer>> GetAllAsync(
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

        public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Customers.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public Task<Customer?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return _context.Customers.FirstOrDefaultAsync(
                x => x.Name == name && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public Task<Customer?> GetByGstNumberAsync(
            string gstNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.Customers.FirstOrDefaultAsync(
                x => x.GstNumber == gstNumber && x.CompanyId == _activeCompany.CompanyId,
                cancellationToken);
        }

        public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            customer.CompanyId = _activeCompany.CompanyId;
            await _context.Customers.AddAsync(customer, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<Customer> BuildQuery(string? search, bool? isActive)
        {
            IQueryable<Customer> query = _context.Customers
                .Where(x => x.CompanyId == _activeCompany.CompanyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    x.Name.Contains(term) ||
                    (x.Phone != null && x.Phone.Contains(term)) ||
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
