using InventoryManagement.Application.DTOs.User;

namespace InventoryManagement.Application.Common.Interfaces
{
    public interface ICompanyMembershipService
    {
        Task<IReadOnlyList<UserCompanyDto>> GetCompaniesForUserAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task<UserCompanyDto?> GetCompanyForUserAsync(
            string userId,
            int companyId,
            CancellationToken cancellationToken = default);

        Task<UserCompanyDto> RequireCompanyAccessAsync(
            string userId,
            int companyId,
            CancellationToken cancellationToken = default);

        Task<UserCompanyDto> RequireCompanyRoleAsync(
            string userId,
            int companyId,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken = default);

        Task<bool> UserHasAnyRoleAsync(
            string userId,
            int companyId,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken = default);
    }
}
