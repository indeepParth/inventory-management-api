using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagement.Application.Common.Identity;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<(bool success, IEnumerable<string> error)> CreateUserAsync(string userName, string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                return (true, Enumerable.Empty<string>());
            }

            return (
                false,
                result.Errors.Select(x => x.Description)
            );
        }

        public async Task<bool> CheckPasswordAsync(string userName, string password)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user is null)
                return false;

            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IList<string>> GetRolesAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user is null)
                return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<string?> GetUserIdAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            return user?.Id;
        }

        public async Task<string?> GetUserNameAsync(string userName)
        {
            var user = await _userManager.FindByIdAsync(userName);

            if (user is null)
                throw new UnauthorizedAccessException("User not found.");

            return user.UserName;
        }
    }
}