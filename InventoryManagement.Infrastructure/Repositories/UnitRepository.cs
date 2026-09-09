using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveCompanyService _activeCompany;

        public UnitRepository(
            ApplicationDbContext context,
            IActiveCompanyService activeCompany)
        {
            _context = context;
            _activeCompany = activeCompany;
        }

        public async Task<List<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(x => x.BaseUnit)
                .Where(x => x.CompanyId == _activeCompany.CompanyId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Unit?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(x => x.BaseUnit)
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.CompanyId == _activeCompany.CompanyId,
                    cancellationToken);
        }

        public async Task<Unit?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(x => x.BaseUnit)
                .FirstOrDefaultAsync(
                    x => x.Name == name && x.CompanyId == _activeCompany.CompanyId,
                    cancellationToken);
        }

        public async Task AddAsync(Unit unit, CancellationToken cancellationToken = default)
        {
            unit.CompanyId = _activeCompany.CompanyId;
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
