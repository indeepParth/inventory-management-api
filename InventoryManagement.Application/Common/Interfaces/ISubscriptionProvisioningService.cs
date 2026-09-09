namespace InventoryManagement.Application.Common.Interfaces
{
    public interface ISubscriptionProvisioningService
    {
        Task EnsureFreeSubscriptionAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }
}
