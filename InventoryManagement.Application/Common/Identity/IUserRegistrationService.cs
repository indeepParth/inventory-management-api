namespace InventoryManagement.Application.Common.Identity
{
    public interface IUserRegistrationService
    {
        Task<(bool Success, IEnumerable<string> Errors)> RegisterAsync(
            string userName,
            string email,
            string password,
            CancellationToken cancellationToken = default);
    }
}
