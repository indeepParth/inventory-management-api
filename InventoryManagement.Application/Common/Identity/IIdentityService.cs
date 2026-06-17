using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Application.Common.Identity
{
    public interface IIdentityService
    {
        Task<(bool success, IEnumerable<string> error)> CreateUserAsync
        (
            string userName,
            string email,
            string password
        );

        Task<bool> CheckPasswordAsync(
            string userName,
            string password);

        Task<IList<string>> GetRolesAsync(
            string userName);

        Task<string?> GetUserIdAsync(
            string userName);

        Task<string?> GetUserNameAsync(
            string userName);
    }
}