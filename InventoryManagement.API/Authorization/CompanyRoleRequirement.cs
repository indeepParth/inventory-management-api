using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.API.Authorization
{
    public class CompanyRoleRequirement : IAuthorizationRequirement
    {
        public CompanyRoleRequirement(params string[] allowedRoles)
        {
            AllowedRoles = allowedRoles;
        }

        public IReadOnlyCollection<string> AllowedRoles { get; }
    }
}
