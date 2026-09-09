using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Identity
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRegistrationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<(bool Success, IEnumerable<string> Errors, int? CompanyId)> RegisterOwnerAsync(
            string userName,
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email
            };

            var createResult = await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                return (
                    false,
                    createResult.Errors.Select(x => x.Description),
                    null);
            }

            var createdAtUtc = DateTime.UtcNow;
            var company = new Company
            {
                Name = $"{userName}'s Company",
                CreatedAtUtc = createdAtUtc
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync(cancellationToken);

            _context.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = company.Id,
                UserId = user.Id,
                Role = CompanyRoles.Owner,
                CreatedAtUtc = createdAtUtc
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (true, Enumerable.Empty<string>(), company.Id);
        }
    }
}
