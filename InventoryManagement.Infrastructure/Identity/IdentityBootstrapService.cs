using InventoryManagement.Application.Authorization;
using InventoryManagement.Application.Common.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Infrastructure.Identity
{
    public class IdentityBootstrapService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AdminBootstrapOptions _adminOptions;
        private readonly IHostEnvironment _environment;

        public IdentityBootstrapService(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IOptions<AdminBootstrapOptions> adminOptions,
            IHostEnvironment environment)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _adminOptions = adminOptions.Value;
            _environment = environment;
        }

        public async Task BootstrapAsync()
        {
            await SeedRolesAsync();

            if (!_adminOptions.Enabled || !_environment.IsProduction())
            {
                return;
            }

            ValidateAdminBootstrapOptions();

            var existingAdmins = await _userManager
                .GetUsersInRoleAsync(ApplicationRoles.Admin);

            if (existingAdmins.Count > 0)
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

            var roleResult = await _userManager.AddToRoleAsync(
                admin,
                ApplicationRoles.Admin);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin bootstrap role assignment failed: " +
                    string.Join(", ", roleResult.Errors.Select(x => x.Description)));
            }
        }

        private async Task SeedRolesAsync()
        {
            foreach (var role in ApplicationRoles.All)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                var result = await _roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Role '{role}' could not be created: " +
                        string.Join(", ", result.Errors.Select(x => x.Description)));
                }
            }
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
