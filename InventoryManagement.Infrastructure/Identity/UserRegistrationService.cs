using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Identity
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDefaultCompanyDataService _defaultCompanyDataService;
        private readonly ISubscriptionProvisioningService _subscriptionProvisioningService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRegistrationService(
            ApplicationDbContext context,
            IDefaultCompanyDataService defaultCompanyDataService,
            ISubscriptionProvisioningService subscriptionProvisioningService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _defaultCompanyDataService = defaultCompanyDataService;
            _subscriptionProvisioningService = subscriptionProvisioningService;
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

            await _subscriptionProvisioningService.EnsureFreeSubscriptionAsync(
                user.Id,
                cancellationToken);

            var createdAtUtc = DateTime.UtcNow;
            var company = new Company
            {
                Name = $"{userName}'s Company",
                BillingOwnerUserId = user.Id,
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
            await _defaultCompanyDataService.SeedDefaultUnitsAsync(
                company.Id,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (true, Enumerable.Empty<string>(), company.Id);
        }
    }
}
