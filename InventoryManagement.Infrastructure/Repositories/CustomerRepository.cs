using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Customer?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return _context.Customers.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        }

        public Task<Customer?> GetByGstNumberAsync(
            string gstNumber,
            CancellationToken cancellationToken = default)
        {
            return _context.Customers.FirstOrDefaultAsync(
                x => x.GstNumber == gstNumber,
                cancellationToken);
        }

        public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            await _context.Customers.AddAsync(customer, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
