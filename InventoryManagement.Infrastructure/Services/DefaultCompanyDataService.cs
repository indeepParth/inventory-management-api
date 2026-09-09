using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services
{
    public class DefaultCompanyDataService : IDefaultCompanyDataService
    {
        private static readonly string[] DefaultUnitNames =
        [
            "Ton",
            "Kilogram",
            "Bag",
            "Piece",
            "Cubic foot",
            "Cubic meter"
        ];

        private readonly ApplicationDbContext _context;

        public DefaultCompanyDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedDefaultUnitsAsync(
            int companyId,
            CancellationToken cancellationToken = default)
        {
            var existingNames = await _context.Units
                .Where(x => x.CompanyId == companyId)
                .Select(x => x.Name)
                .ToListAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var units = DefaultUnitNames
                .Where(name => !existingNames.Contains(name))
                .Select(name => new Unit
                {
                    CompanyId = companyId,
                    Name = name,
                    FactorToBaseUnit = 1m,
                    IsActive = true,
                    CreatedAtUtc = now
                })
                .ToList();

            if (units.Count == 0)
            {
                return;
            }

            _context.Units.AddRange(units);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var unit in units)
            {
                unit.BaseUnitId = unit.Id;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
