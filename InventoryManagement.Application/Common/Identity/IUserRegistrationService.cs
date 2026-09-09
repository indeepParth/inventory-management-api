namespace InventoryManagement.Application.Common.Identity
{
    public interface IUserRegistrationService
    {
        Task<(bool Success, IEnumerable<string> Errors, int? CompanyId)> RegisterOwnerAsync(
            string userName,
            string email,
            string password,
            CancellationToken cancellationToken = default);
    }
}
