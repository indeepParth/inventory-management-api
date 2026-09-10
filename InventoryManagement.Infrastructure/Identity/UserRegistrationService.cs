using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Infrastructure.Identity
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionProvisioningService _subscriptionProvisioningService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRegistrationService(
            ApplicationDbContext context,
            ISubscriptionProvisioningService subscriptionProvisioningService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _subscriptionProvisioningService = subscriptionProvisioningService;
            _userManager = userManager;
        }

        public async Task<(bool Success, IEnumerable<string> Errors)> RegisterAsync(
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
                    createResult.Errors.Select(x => x.Description));
            }

            await _subscriptionProvisioningService.EnsureFreeSubscriptionAsync(
                user.Id,
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return (true, Enumerable.Empty<string>());
        }
    }
}
