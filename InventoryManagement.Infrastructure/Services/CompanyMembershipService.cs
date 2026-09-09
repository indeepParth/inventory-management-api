using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.DTOs.User;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Services
{
    public class CompanyMembershipService : ICompanyMembershipService
    {
        private readonly ApplicationDbContext _context;

        public CompanyMembershipService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<UserCompanyDto>> GetCompaniesForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.CompanyUsers
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Company.Name)
                .Select(x => new UserCompanyDto
                {
                    Id = x.CompanyId,
                    Name = x.Company.Name,
                    Role = x.Role
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> UserHasAnyRoleAsync(
            string userId,
            int companyId,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken = default)
        {
            return await _context.CompanyUsers
                .AsNoTracking()
                .AnyAsync(
                    x => x.UserId == userId &&
                         x.CompanyId == companyId &&
                         (x.Role == CompanyRoles.Owner ||
                          allowedRoles.Contains(x.Role)),
                    cancellationToken);
        }
    }
}
