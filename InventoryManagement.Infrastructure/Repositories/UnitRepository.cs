using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly ApplicationDbContext _context;

        public UnitRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(x => x.BaseUnit)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Unit?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(x => x.BaseUnit)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<Unit?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(x => x.BaseUnit)
                .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        }

        public async Task AddAsync(Unit unit, CancellationToken cancellationToken = default)
        {
            await _context.Units.AddAsync(unit, cancellationToken);
        }

        public async Task DeleteAsync(Unit unit, CancellationToken cancellationToken = default)
        {
            _context.Units.Remove(unit);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
