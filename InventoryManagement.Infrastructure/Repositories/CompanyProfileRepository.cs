using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class CompanyProfileRepository : ICompanyProfileRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveCompanyService _activeCompany;

        public CompanyProfileRepository(
            ApplicationDbContext context,
            IActiveCompanyService activeCompany)
        {
            _context = context;
            _activeCompany = activeCompany;
        }

        public Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken = default)
        {
            return _context.CompanyProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.CompanyId == _activeCompany.CompanyId,
                    cancellationToken);
        }

        public async Task UpsertAsync(
            CompanyProfile companyProfile,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.CompanyProfiles
                .SingleOrDefaultAsync(
                    x => x.CompanyId == _activeCompany.CompanyId,
                    cancellationToken);
            var now = DateTime.UtcNow;

            if (existing is null)
            {
                companyProfile.CompanyId = _activeCompany.CompanyId;
                companyProfile.CreatedAtUtc = now;
                companyProfile.UpdatedAtUtc = now;
                _context.CompanyProfiles.Add(companyProfile);
            }
            else
            {
                existing.CompanyName = companyProfile.CompanyName;
                existing.Address = companyProfile.Address;
                existing.GstNumber = companyProfile.GstNumber;
                existing.Email = companyProfile.Email;
                existing.Phone = companyProfile.Phone;
                existing.Website = companyProfile.Website;
                existing.UpdatedAtUtc = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
