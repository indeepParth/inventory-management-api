using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories
{
    public class CompanyProfileRepository : ICompanyProfileRepository
    {
        private const int SingletonId = 1;
        private readonly ApplicationDbContext _context;

        public CompanyProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<CompanyProfile?> GetAsync(CancellationToken cancellationToken = default)
        {
            return _context.CompanyProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == SingletonId, cancellationToken);
        }

        public async Task UpsertAsync(
            CompanyProfile companyProfile,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.CompanyProfiles
                .SingleOrDefaultAsync(x => x.Id == SingletonId, cancellationToken);
            var now = DateTime.UtcNow;

            if (existing is null)
            {
                companyProfile.Id = SingletonId;
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
