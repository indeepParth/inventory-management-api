using InventoryManagement.Application.Common.Exceptions;
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

        public async Task<UserCompanyDto?> GetCompanyForUserAsync(
            string userId,
            int companyId,
            CancellationToken cancellationToken = default)
        {
            return await _context.CompanyUsers
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.CompanyId == companyId)
                .Select(x => new UserCompanyDto
                {
                    Id = x.CompanyId,
                    Name = x.Company.Name,
                    Role = x.Role
                })
                .SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<UserCompanyDto> RequireCompanyAccessAsync(
            string userId,
            int companyId,
            CancellationToken cancellationToken = default)
        {
            var company = await GetCompanyForUserAsync(
                userId,
                companyId,
                cancellationToken);

            if (company is null)
            {
                throw new ForbiddenException("Company access is required.");
            }

            return company;
        }

        public async Task<UserCompanyDto> RequireCompanyRoleAsync(
            string userId,
            int companyId,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken = default)
        {
            var company = await RequireCompanyAccessAsync(
                userId,
                companyId,
                cancellationToken);

            if (!allowedRoles.Contains(company.Role))
            {
                throw new ForbiddenException("Required company role is missing.");
            }

            return company;
        }

        public async Task<bool> UserHasAnyRoleAsync(
            string userId,
            int companyId,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken = default)
        {
            var company = await GetCompanyForUserAsync(
                userId,
                companyId,
                cancellationToken);

            return company is not null && allowedRoles.Contains(company.Role);
        }
    }
}
