using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Options;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Infrastructure.Identity
{
    public class IdentityBootstrapService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionProvisioningService _subscriptionProvisioningService;
        private readonly AdminBootstrapOptions _adminOptions;
        private readonly IHostEnvironment _environment;

        public IdentityBootstrapService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ISubscriptionProvisioningService subscriptionProvisioningService,
            IOptions<AdminBootstrapOptions> adminOptions,
            IHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _subscriptionProvisioningService = subscriptionProvisioningService;
            _adminOptions = adminOptions.Value;
            _environment = environment;
        }

        public async Task BootstrapAsync()
        {
            if (!_adminOptions.Enabled ||
                (!_environment.IsProduction() &&
                 !_adminOptions.AllowOutsideProduction))
            {
                return;
            }

            ValidateAdminBootstrapOptions();

            var existingOwners = _context.CompanyUsers
                .Where(x => x.Role == CompanyRoles.Owner)
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

            if (existingOwners.Count > 0)
            {
                return;
            }

            var existingUserByName = await _userManager.FindByNameAsync(
                _adminOptions.UserName);

            var existingUserByEmail = await _userManager.FindByEmailAsync(
                _adminOptions.Email);

            if (existingUserByName is not null || existingUserByEmail is not null)
            {
                throw new InvalidOperationException(
                    "Admin bootstrap user already exists. Existing users are not modified.");
            }

            var admin = new ApplicationUser
            {
                UserName = _adminOptions.UserName,
                Email = _adminOptions.Email
            };

            var createResult = await _userManager.CreateAsync(
                admin,
                _adminOptions.Password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin bootstrap user could not be created: " +
                    string.Join(", ", createResult.Errors.Select(x => x.Description)));
            }

            await _subscriptionProvisioningService.EnsureFreeSubscriptionAsync(
                admin.Id);

            var createdAtUtc = DateTime.UtcNow;
            var company = new Company
            {
                Name = $"{_adminOptions.UserName}'s Company",
                BillingOwnerUserId = admin.Id,
                CreatedAtUtc = createdAtUtc
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            _context.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = company.Id,
                UserId = admin.Id,
                Role = CompanyRoles.Owner,
                CreatedAtUtc = createdAtUtc
            });

            await _context.SaveChangesAsync();
        }

        private void ValidateAdminBootstrapOptions()
        {
            var missingSettings = new List<string>();

            if (string.IsNullOrWhiteSpace(_adminOptions.UserName))
            {
                missingSettings.Add("Bootstrap:Admin:UserName");
            }

            if (string.IsNullOrWhiteSpace(_adminOptions.Email))
            {
                missingSettings.Add("Bootstrap:Admin:Email");
            }

            if (string.IsNullOrWhiteSpace(_adminOptions.Password))
            {
                missingSettings.Add("Bootstrap:Admin:Password");
            }

            if (missingSettings.Count > 0)
            {
                throw new InvalidOperationException(
                    "Admin bootstrap configuration is invalid. Missing settings: " +
                    string.Join(", ", missingSettings));
            }
        }
    }
}
